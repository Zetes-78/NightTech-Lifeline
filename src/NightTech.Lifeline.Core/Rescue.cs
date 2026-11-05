using System.Text;

namespace NightTech.Lifeline.Core;

/// <summary>What one press of "Get me out of this scam screen" did.</summary>
public sealed class RescueResult
{
    public DateTime When { get; init; }
    /// <summary>Browser name and how many of its windows/processes were closed.</summary>
    public Dictionary<string, int> BrowsersClosed { get; } = new(StringComparer.Ordinal);
    /// <summary>Browsers that would not close (for example one started as administrator).</summary>
    public List<string> BrowsersStillOpen { get; } = new();
    public List<MoveItem> TabsSaved { get; } = new();
    public List<string> TabsNotMoved { get; } = new();
    public string SavedFolder { get; set; } = "";

    public bool ClosedAnything => BrowsersClosed.Count > 0;
}

/// <summary>What "Someone is on my computer" did.</summary>
public sealed class CutOffResult
{
    public DateTime When { get; init; }
    public List<string> RemoteToolsClosed { get; } = new();
    /// <summary>Remote tools that are running but couldn't be closed from here (usually installed as a service).</summary>
    public List<string> RemoteToolsStillRunning { get; } = new();
    public bool? WifiTurnedOff { get; set; }
    public string WifiNote { get; set; } = "";
}

/// <summary>The words Lifeline shows. Calm, short and plain, written for someone who is scared.</summary>
public static class Words
{
    public const string HotkeyText = "Ctrl + Alt + H";

    public const string RescueButton = "Get me out of this scam screen";
    public const string RescueButtonSub = "Closes your web browsers so the warning goes away";
    public const string CutOffButton = "Someone is on my computer";
    public const string CutOffButtonSub = "Closes remote-control apps and turns off Wi-Fi";

    public const string ConfirmRescue =
        "Lifeline will close all your web browsers right now, including any other tabs you have open.\n\n" +
        "Your tabs are saved, not deleted. Night Tech can bring them back for you.\n\nClose the browsers?";

    public const string ConfirmCutOff =
        "Lifeline will close remote-control apps such as AnyDesk and TeamViewer, and turn off Wi-Fi so no one can stay connected.\n\n" +
        "If someone from Night Tech is helping you right now, choose No.\n\nCut them off?";

    public static readonly string[] NeverDo =
    {
        "Don't call any phone number that a pop-up or warning shows you.",
        "Don't pay anyone with gift cards, wire transfers, cryptocurrency or a payment app.",
        "Don't let anyone you don't know connect to your computer.",
        "Don't give out passwords or codes sent to your phone.",
    };

    public static readonly string[] Facts =
    {
        "Microsoft, Apple, your bank, the IRS and Social Security will never lock your screen.",
        "They will never call you out of the blue to fix your computer.",
        "A real warning never asks you to call a phone number.",
    };

    public static readonly string[] IfAlreadyActed =
    {
        "If you paid or gave card or bank details: call your bank using the number on the back of your card.",
        "If you let someone connect: press \"Someone is on my computer\" now, then call Night Tech.",
        "If you gave a password: call Night Tech so we can help you change it from a safe device.",
    };

    public static string RescueSummary(RescueResult r)
    {
        if (!r.ClosedAnything && r.BrowsersStillOpen.Count == 0)
            return "No web browsers were open. If a scary message is still on the screen, hold the power button until the computer turns off, then call Night Tech.";
        var sb = new StringBuilder();
        if (r.ClosedAnything) sb.Append("You're safe now. We closed ").Append(JoinNames(r.BrowsersClosed.Keys)).Append('.');
        if (r.BrowsersStillOpen.Count > 0)
            sb.Append(sb.Length > 0 ? " " : "").Append(JoinNames(r.BrowsersStillOpen)).Append(" would not close. Hold the power button until the computer turns off, then call Night Tech.");
        else if (r.TabsSaved.Count > 0)
            sb.Append(" When you open your browser again, the scam page won't come back.");
        if (r.TabsNotMoved.Count > 0)
            sb.Append(" Some saved tabs couldn't be moved, so the page might reopen. If it does, press this button again.");
        return sb.ToString();
    }

    public static string CutOffSummary(CutOffResult r)
    {
        var parts = new List<string>();
        if (r.RemoteToolsClosed.Count > 0) parts.Add("We closed " + JoinNames(r.RemoteToolsClosed) + ".");
        if (r.RemoteToolsStillRunning.Count > 0)
            parts.Add(JoinNames(r.RemoteToolsStillRunning) + " is still running in the background. Unplug the internet cable or turn off the computer, then call Night Tech.");
        if (r.RemoteToolsClosed.Count == 0 && r.RemoteToolsStillRunning.Count == 0) parts.Add("We didn't find any remote-control apps running.");
        parts.Add(r.WifiTurnedOff switch
        {
            true => "Wi-Fi is off. If your computer uses an internet cable, unplug it too.",
            false => "We couldn't turn off Wi-Fi" + (r.WifiNote.Length > 0 ? " (" + r.WifiNote + ")" : "") + ". Unplug the internet cable or turn off your router.",
            null => "If your computer uses an internet cable, unplug it.",
        });
        return string.Join(" ", parts);
    }

    /// <summary>"Chrome", "Chrome and Edge", "Chrome, Edge and Firefox".</summary>
    public static string JoinNames(IEnumerable<string> names)
    {
        var l = names.Distinct(StringComparer.Ordinal).ToList();
        return l.Count switch { 0 => "", 1 => l[0], _ => string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1] };
    }

    /// <summary>A line for the local activity log, so a Night Tech technician can see what happened.</summary>
    public static string LogLine(RescueResult r) =>
        $"{r.When:yyyy-MM-dd HH:mm:ss} RESCUE closed=[{string.Join(", ", r.BrowsersClosed.Select(kv => kv.Key + " x" + kv.Value))}] stillOpen=[{string.Join(", ", r.BrowsersStillOpen)}] saved={r.TabsSaved.Count} notMoved={r.TabsNotMoved.Count} folder=\"{r.SavedFolder}\"";

    public static string LogLine(CutOffResult r) =>
        $"{r.When:yyyy-MM-dd HH:mm:ss} CUTOFF closed=[{string.Join(", ", r.RemoteToolsClosed)}] stillRunning=[{string.Join(", ", r.RemoteToolsStillRunning)}] wifiOff={(r.WifiTurnedOff?.ToString() ?? "unknown")} {r.WifiNote}".TrimEnd();
}
