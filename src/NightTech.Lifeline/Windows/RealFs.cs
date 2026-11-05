using System;
using System.Collections.Generic;
using System.IO;
using NightTech.Lifeline.Core;

namespace NightTech.Lifeline.Windows;

public sealed class RealFs : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> GetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return Array.Empty<string>(); }
    }

    public bool IsReparsePoint(string path)
    {
        try { return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return true; }   // unreadable: treat as unsafe
    }

    public string Combine(string a, string b) => Path.Combine(a, b);
    public string GetFileName(string path) => Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
}
