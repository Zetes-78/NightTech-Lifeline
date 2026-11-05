using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NightTech.Lifeline.Core;

namespace NightTech.Lifeline.Windows;

/// <summary>Opens web pages and phone links. Only Night Tech's own links and the family number (digits only) are allowed.</summary>
public static class Links
{
    private static readonly Regex FamilyLink = new(@"^(tel|sms):\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant);

    public static bool IsAllowed(string link) => Brand.AllowedLinks.Contains(link) || FamilyLink.IsMatch(link);

    /// <summary>True if Windows has an app for tel: or sms: links (Phone Link, Teams, Skype...). Otherwise we show the number.</summary>
    public static bool CanOpen(string scheme)
    {
        try
        {
            using var k = Registry.ClassesRoot.OpenSubKey(scheme);
            if (k?.GetValue("URL Protocol") is null) return false;
            using var cmd = Registry.ClassesRoot.OpenSubKey(scheme + @"\shell\open\command");
            return cmd?.GetValue(null) is string s && s.Length > 0;
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException) { return false; }
    }

    public static bool Open(string link)
    {
        if (!IsAllowed(link)) return false;
        try
        {
            using var p = Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            return true;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { return false; }
    }

    /// <summary>Opens the saved-tabs folder in File Explorer (a fixed folder, for a technician helping the person).</summary>
    public static void OpenFolder(string path)
    {
        try
        {
            System.IO.Directory.CreateDirectory(path);
            using var p = Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = false, ArgumentList = { path } });
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or System.IO.IOException or UnauthorizedAccessException) { }
    }
}
