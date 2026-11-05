namespace NightTech.Lifeline.Core;

/// <summary>The few file operations the session finder needs, so it can be tested without a real disk.</summary>
public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    IEnumerable<string> GetDirectories(string path);
    /// <summary>True for links and junctions. Lifeline never follows or moves them.</summary>
    bool IsReparsePoint(string path);
    string Combine(string a, string b);
    string GetFileName(string path);
}

/// <summary>One file or folder to move out of a browser's profile, and where its saved copy goes.</summary>
public sealed record MoveItem(string Browser, string Source, string Destination, bool IsDirectory);

/// <summary>
/// Finds the files a browser uses to reopen the last tabs. A scam page that took over the screen comes straight back
/// when the browser restarts, so Lifeline moves these aside (never deletes them) after closing the browser.
/// </summary>
public static class SessionFiles
{
    public static readonly string[] ChromiumFiles = { "Current Session", "Current Tabs", "Last Session", "Last Tabs" };
    public const string ChromiumFolder = "Sessions";
    public const string FirefoxFile = "sessionstore.jsonlz4";
    public const string FirefoxFolder = "sessionstore-backups";

    public static string DataFolder(Browser b, string localAppData, string roamingAppData, IFileSystem fs) =>
        fs.Combine(b.Root == DataRoot.LocalAppData ? localAppData : roamingAppData, b.DataPath);

    /// <summary>Chromium profile folders are "Default", "Profile 1", "Profile 2"... and "Guest Profile".</summary>
    public static bool IsChromiumProfile(string folderName) =>
        folderName == "Default" || folderName == "Guest Profile" ||
        (folderName.StartsWith("Profile ", StringComparison.Ordinal) && folderName.Length > 8 && folderName[8..].All(char.IsAsciiDigit));

    public static List<MoveItem> Find(Browser b, string localAppData, string roamingAppData, string backupRoot, IFileSystem fs)
    {
        var items = new List<MoveItem>();
        if (b.Kind == BrowserKind.NoSession || b.DataPath.Length == 0) return items;
        var root = DataFolder(b, localAppData, roamingAppData, fs);
        if (!fs.DirectoryExists(root) || fs.IsReparsePoint(root)) return items;

        IEnumerable<string> profiles = b.ProfileIsRoot
            ? new[] { root }
            : fs.GetDirectories(root).Where(d => b.Kind == BrowserKind.Firefox || IsChromiumProfile(fs.GetFileName(d)));

        var backup = fs.Combine(backupRoot, SafeName(b.Name));
        foreach (var profile in profiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (fs.IsReparsePoint(profile)) continue;
            var dest = b.ProfileIsRoot ? backup : fs.Combine(backup, SafeName(fs.GetFileName(profile)));
            void Add(string name, bool dir)
            {
                var p = fs.Combine(profile, name);
                bool exists = dir ? fs.DirectoryExists(p) : fs.FileExists(p);
                if (exists && !fs.IsReparsePoint(p)) items.Add(new MoveItem(b.Name, p, fs.Combine(dest, name), dir));
            }
            if (b.Kind == BrowserKind.Chromium)
            {
                Add(ChromiumFolder, true);
                foreach (var f in ChromiumFiles) Add(f, false);
            }
            else
            {
                Add(FirefoxFile, false);
                Add(FirefoxFolder, true);
            }
        }
        return items;
    }

    /// <summary>A folder name built from a browser or profile name: no path separators or characters Windows refuses.</summary>
    public static string SafeName(string name)
    {
        var bad = new HashSet<char>("<>:\"/\\|?*");
        var s = new string((name ?? "").Select(c => c < 32 || bad.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (s.Length == 0 || s == "." || s == "..") s = "_";
        return s.Length > 60 ? s[..60] : s;
    }

    /// <summary>The saved-tabs folder for one rescue: "2026-11-05 143012".</summary>
    public static string StampFolder(DateTime local) => local.ToString("yyyy-MM-dd HHmmss", System.Globalization.CultureInfo.InvariantCulture);
}
