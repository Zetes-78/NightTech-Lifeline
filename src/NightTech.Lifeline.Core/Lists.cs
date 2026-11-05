namespace NightTech.Lifeline.Core;

public enum BrowserKind { Chromium, Firefox, NoSession }
public enum DataRoot { LocalAppData, RoamingAppData }

/// <summary>
/// A browser Lifeline can close. <see cref="DataPath"/> is where it keeps its profiles, relative to the person's
/// AppData folder. When <see cref="ProfileIsRoot"/> is true (Opera), that folder is itself the profile.
/// </summary>
public sealed record Browser(string Name, string[] ProcessNames, BrowserKind Kind, DataRoot Root, string DataPath, bool ProfileIsRoot = false);

/// <summary>A remote-control app scammers use to get onto a PC.</summary>
public sealed record RemoteTool(string Name, string[] ProcessNames);

public static class Lists
{
    public static readonly IReadOnlyList<Browser> Browsers = new[]
    {
        new Browser("Google Chrome", new[] { "chrome" }, BrowserKind.Chromium, DataRoot.LocalAppData, @"Google\Chrome\User Data"),
        new Browser("Microsoft Edge", new[] { "msedge" }, BrowserKind.Chromium, DataRoot.LocalAppData, @"Microsoft\Edge\User Data"),
        new Browser("Brave", new[] { "brave" }, BrowserKind.Chromium, DataRoot.LocalAppData, @"BraveSoftware\Brave-Browser\User Data"),
        new Browser("Vivaldi", new[] { "vivaldi" }, BrowserKind.Chromium, DataRoot.LocalAppData, @"Vivaldi\User Data"),
        new Browser("Opera", new[] { "opera" }, BrowserKind.Chromium, DataRoot.RoamingAppData, @"Opera Software\Opera Stable", ProfileIsRoot: true),
        new Browser("Opera GX", new[] { "opera" }, BrowserKind.Chromium, DataRoot.RoamingAppData, @"Opera Software\Opera GX Stable", ProfileIsRoot: true),
        new Browser("Firefox", new[] { "firefox" }, BrowserKind.Firefox, DataRoot.RoamingAppData, @"Mozilla\Firefox\Profiles"),
        new Browser("Internet Explorer", new[] { "iexplore" }, BrowserKind.NoSession, DataRoot.LocalAppData, ""),
    };

    /// <summary>
    /// Remote-control apps. Lifeline only closes copies running as the signed-in person; ones installed as a Windows
    /// service run as the system and are reported instead. Night Tech's own Sentinel is deliberately not here.
    /// </summary>
    public static readonly IReadOnlyList<RemoteTool> RemoteTools = new[]
    {
        new RemoteTool("AnyDesk", new[] { "anydesk" }),
        new RemoteTool("TeamViewer", new[] { "teamviewer", "teamviewer_desktop", "teamviewer_service", "tv_w32", "tv_x64", "teamviewerqs" }),
        new RemoteTool("UltraViewer", new[] { "ultraviewer_desktop", "ultraviewer_service" }),
        new RemoteTool("ScreenConnect", new[] { "screenconnect.windowsclient", "screenconnect.clientservice" }),
        new RemoteTool("RustDesk", new[] { "rustdesk" }),
        new RemoteTool("Supremo", new[] { "supremo", "supremoservice" }),
        new RemoteTool("AeroAdmin", new[] { "aeroadmin" }),
        new RemoteTool("Ammyy Admin", new[] { "aa_v3" }),
        new RemoteTool("Quick Assist", new[] { "quickassist" }),
        new RemoteTool("Chrome Remote Desktop", new[] { "remoting_host", "remoting_desktop" }),
        new RemoteTool("LogMeIn", new[] { "logmein", "logmeinsystray", "lmiguardiansvc", "logmeinguardsvc" }),
        new RemoteTool("Splashtop", new[] { "srservice", "srfeature", "srmanager", "strwinclt", "splashtopsos" }),
        new RemoteTool("AnyViewer", new[] { "anyviewer", "rcclient", "rcservice" }),
        new RemoteTool("HopToDesk", new[] { "hoptodesk" }),
        new RemoteTool("Remote Utilities", new[] { "rutserv", "rfusclient" }),
        new RemoteTool("AweSun", new[] { "awesun" }),
        new RemoteTool("ToDesk", new[] { "todesk", "todesk_service" }),
        new RemoteTool("RemotePC", new[] { "remotepcservice" }),
        new RemoteTool("Zoho Assist", new[] { "za_connect", "zohotray" }),
    };

    /// <summary>Which remote tool a running process belongs to, or null. Names are compared without ".exe" and ignoring case.</summary>
    public static RemoteTool? RemoteToolFor(string processName)
    {
        var n = Normalize(processName);
        return RemoteTools.FirstOrDefault(t => t.ProcessNames.Contains(n, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>The browsers that use this process name (Opera and Opera GX share one).</summary>
    public static IEnumerable<Browser> BrowsersFor(string processName)
    {
        var n = Normalize(processName);
        return Browsers.Where(b => b.ProcessNames.Contains(n, StringComparer.OrdinalIgnoreCase));
    }

    public static string Normalize(string processName)
    {
        var n = (processName ?? "").Trim();
        if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) n = n[..^4];
        return n.ToLowerInvariant();
    }
}
