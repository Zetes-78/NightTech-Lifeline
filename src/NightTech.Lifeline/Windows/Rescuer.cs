using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NightTech.Lifeline.Core;

namespace NightTech.Lifeline.Windows;

/// <summary>
/// "Get me out of this scam screen": closes the person's web browsers, then moves the browsers' reopen-my-tabs files
/// aside so the scam page doesn't come straight back. Only programs in this Windows session are touched, and files are
/// moved into Lifeline's own folder, never deleted.
/// </summary>
public static class Rescuer
{
    public static RescueResult Run()
    {
        var result = new RescueResult { When = DateTime.Now };
        int session = CurrentSession();
        var closedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var killed = new List<(int Pid, string Name)>();

        // Close every browser process in this session. Browsers run many processes; any one left keeps the window alive.
        // First take hold of each one (an open handle stops Windows giving its number to a new program), then close them,
        // so closing the main window's process can never lead to closing some other program that reused a number.
        var targets = new List<(Process P, string Name, string Label)>();
        foreach (var p in SafeProcesses())
        {
            var name = SafeName(p);
            if (Lists.BrowsersFor(name).Any() && InSession(p, session) && Hold(p)) targets.Add((p, Lists.Normalize(name), Label(name)));
            else p.Dispose();
        }
        foreach (var (p, name, label) in targets)
        {
            using (p)
            {
                try
                {
                    killed.Add((p.Id, name));
                    p.Kill();
                    result.BrowsersClosed[label] = result.BrowsersClosed.TryGetValue(label, out var n) ? n + 1 : 1;
                    closedNames.Add(name);
                }
                catch (Exception e) when (e is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    if (!p.HasExitedSafe() && !result.BrowsersStillOpen.Contains(label)) result.BrowsersStillOpen.Add(label);
                }
            }
        }

        // Wait (up to about 6 seconds) for those processes to finish closing, so their files are free to move. Only the
        // processes we closed count: Edge can start a new background copy of itself straight away, and that one has no window.
        var deadline = DateTime.UtcNow.AddSeconds(6);
        while (DateTime.UtcNow < deadline && StillRunning(killed).Count > 0) Thread.Sleep(300);
        foreach (var name in StillRunning(killed))
        {
            var label = Label(name);
            if (!result.BrowsersStillOpen.Contains(label)) result.BrowsersStillOpen.Add(label);
        }
        result.BrowsersStillOpen.Sort(StringComparer.Ordinal);

        if (closedNames.Count == 0) { Store.Log(Words.LogLine(result)); return result; }

        // Move the session files of the browsers we closed.
        var backupRoot = Path.Combine(Store.SavedTabsFolder, SessionFiles.StampFolder(result.When));
        result.SavedFolder = backupRoot;
        var fs = new RealFs();
        foreach (var b in Lists.Browsers.Where(b => b.ProcessNames.Any(closedNames.Contains)))
            foreach (var item in SessionFiles.Find(b, Store.LocalAppData, Store.RoamingAppData, backupRoot, fs))
            {
                if (Move(item, backupRoot)) result.TabsSaved.Add(item);
                else result.TabsNotMoved.Add(item.Source);
            }

        Store.Log(Words.LogLine(result));
        return result;
    }

    /// <summary>The name to show for a browser process. Opera and Opera GX share "opera".</summary>
    private static string Label(string processName)
    {
        var n = Lists.Normalize(processName);
        return n == "opera" ? "Opera" : Lists.BrowsersFor(n).Select(b => b.Name).FirstOrDefault() ?? n;
    }

    private static bool Move(MoveItem item, string backupRoot)
    {
        var root = Path.GetFullPath(backupRoot) + Path.DirectorySeparatorChar;
        var dest = Path.GetFullPath(item.Destination);
        if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                var target = Unused(dest);
                if (item.IsDirectory) Directory.Move(item.Source, target);
                else File.Move(item.Source, target);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (!(item.IsDirectory ? Directory.Exists(item.Source) : File.Exists(item.Source))) return true;   // already gone
                Thread.Sleep(500);
            }
        }
        return false;
    }

    private static string Unused(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return path;
        for (int i = 2; i < 100; i++)
        {
            var p = path + $" ({i})";
            if (!File.Exists(p) && !Directory.Exists(p)) return p;
        }
        return path + " (" + Guid.NewGuid().ToString("N")[..8] + ")";
    }

    private static List<string> StillRunning(List<(int Pid, string Name)> killed)
    {
        var left = new List<string>();
        foreach (var (pid, name) in killed)
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                if (!p.HasExitedSafe() && Lists.Normalize(SafeName(p)) == name && !left.Contains(name)) left.Add(name);
            }
            catch (ArgumentException) { }            // gone
            catch (InvalidOperationException) { }
        }
        return left;
    }

    /// <summary>Opens a handle to the process now. Returns false if it's already gone or can't be opened (then it's left alone).</summary>
    internal static bool Hold(Process p)
    {
        try { return !p.SafeHandle.IsInvalid; }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or NotSupportedException) { return false; }
    }

    internal static int CurrentSession()
    {
        using var me = Process.GetCurrentProcess();
        return me.SessionId;
    }

    internal static Process[] SafeProcesses()
    {
        try { return Process.GetProcesses(); }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException) { return Array.Empty<Process>(); }
    }

    internal static string SafeName(Process p)
    {
        try { return p.ProcessName; }
        catch (Exception e) when (e is InvalidOperationException or Win32Exception or NotSupportedException) { return ""; }
    }

    internal static bool InSession(Process p, int session)
    {
        try { return p.SessionId == session; }
        catch (Exception e) when (e is InvalidOperationException or Win32Exception or NotSupportedException) { return false; }
    }

    internal static bool HasExitedSafe(this Process p)
    {
        try { return p.HasExited; }
        catch (Exception e) when (e is InvalidOperationException or Win32Exception or NotSupportedException) { return false; }
    }
}
