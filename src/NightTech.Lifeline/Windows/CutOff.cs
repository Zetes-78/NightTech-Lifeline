using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NightTech.Lifeline.Core;
using global::Windows.Devices.Radios;

namespace NightTech.Lifeline.Windows;

/// <summary>
/// "Someone is on my computer": closes remote-control apps running as this person and turns Wi-Fi off. Copies running
/// as a Windows service (another session, or the system) can't be closed without administrator rights, so they're
/// reported and the person is told to unplug the internet.
/// </summary>
public static class CutOff
{
    public static async Task<CutOffResult> RunAsync()
    {
        var result = new CutOffResult { When = DateTime.Now };
        int session = Rescuer.CurrentSession();

        // Wi-Fi goes off first, so a background service can't hand the connection back while apps are being closed.
        try
        {
            var access = await Radio.RequestAccessAsync();
            if (access != RadioAccessStatus.Allowed)
            {
                result.WifiTurnedOff = false;
                result.WifiNote = "Windows didn't allow it";
            }
            else
            {
                var wifi = (await Radio.GetRadiosAsync()).Where(r => r.Kind == RadioKind.WiFi).ToList();
                if (wifi.Count == 0) { result.WifiTurnedOff = null; }
                else
                {
                    bool all = true;
                    foreach (var r in wifi)
                        if (r.State == RadioState.On && await r.SetStateAsync(RadioState.Off) != RadioAccessStatus.Allowed) all = false;
                    result.WifiTurnedOff = all;
                    if (!all) result.WifiNote = "Windows didn't allow it";
                }
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            result.WifiTurnedOff = false;
            result.WifiNote = "this computer doesn't allow it";
        }

        // Then close the remote-control apps. Take hold of each first (see Rescuer), then close.
        var found = new List<(Process P, RemoteTool Tool)>();
        foreach (var p in Rescuer.SafeProcesses())
        {
            var tool = Lists.RemoteToolFor(Rescuer.SafeName(p));
            if (tool is null) { p.Dispose(); continue; }
            if (Rescuer.InSession(p, session)) Rescuer.Hold(p);
            found.Add((p, tool));
        }
        foreach (var (p, tool) in found)
        {
            using (p)
            {
                bool closed = false;
                if (Rescuer.InSession(p, session))
                {
                    try { p.Kill(); closed = true; }
                    catch (Exception e) when (e is Win32Exception or InvalidOperationException or NotSupportedException) { closed = p.HasExitedSafe(); }
                }
                if (closed) { if (!result.RemoteToolsClosed.Contains(tool.Name)) result.RemoteToolsClosed.Add(tool.Name); }
                else if (!result.RemoteToolsStillRunning.Contains(tool.Name)) result.RemoteToolsStillRunning.Add(tool.Name);
            }
        }
        // A tool closed in this session may still have a service running elsewhere; list it once, as still running.
        result.RemoteToolsClosed.RemoveAll(result.RemoteToolsStillRunning.Contains);

        Store.Log(Words.LogLine(result));
        return result;
    }

    /// <summary>Turns Wi-Fi back on afterwards, once Night Tech says it's safe.</summary>
    public static async Task<bool> WifiOnAsync()
    {
        try
        {
            if (await Radio.RequestAccessAsync() != RadioAccessStatus.Allowed) return false;
            bool ok = true;
            foreach (var r in (await Radio.GetRadiosAsync()).Where(r => r.Kind == RadioKind.WiFi))
                if (r.State == RadioState.Off && await r.SetStateAsync(RadioState.On) != RadioAccessStatus.Allowed) ok = false;
            return ok;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return false;
        }
    }
}
