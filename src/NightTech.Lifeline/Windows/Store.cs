using System;
using System.IO;
using System.Text;
using NightTech.Lifeline.Core;

namespace NightTech.Lifeline.Windows;

/// <summary>Where Lifeline keeps its few files, all inside the person's own AppData. Nothing is ever sent anywhere.</summary>
public static class Store
{
    public static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public static string RoamingAppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string Folder => Path.Combine(LocalAppData, "Night Tech Solutions", "Lifeline");
    public static string SavedTabsFolder => Path.Combine(Folder, "Saved tabs");
    private static string SettingsFile => Path.Combine(Folder, "settings.json");
    private static string LogFile => Path.Combine(Folder, "activity.log");
    private const long MaxLog = 200 * 1024;

    public static Settings LoadSettings()
    {
        try
        {
            var f = new FileInfo(SettingsFile);
            if (!f.Exists || f.Length > 64 * 1024 || f.Attributes.HasFlag(FileAttributes.ReparsePoint)) return new Settings();
            return Settings.FromJson(File.ReadAllText(f.FullName, Encoding.UTF8));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return new Settings(); }
    }

    /// <summary>Writes to a temporary file first, so a crash never leaves half a settings file.</summary>
    public static bool SaveSettings(Settings s)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var tmp = SettingsFile + ".tmp";
            File.WriteAllText(tmp, s.ToJson(), new UTF8Encoding(false));
            File.Move(tmp, SettingsFile, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    public static void Log(string line)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var f = new FileInfo(LogFile);
            if (f.Exists && f.Length > MaxLog) File.Move(LogFile, LogFile + ".1", overwrite: true);
            File.AppendAllText(LogFile, line.Replace('\r', ' ').Replace('\n', ' ') + Environment.NewLine, new UTF8Encoding(false));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
