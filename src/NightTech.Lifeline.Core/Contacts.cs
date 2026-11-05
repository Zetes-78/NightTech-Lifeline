using System.Text.Json;
using System.Text.Json.Serialization;

namespace NightTech.Lifeline.Core;

/// <summary>Phone numbers typed in by the family. Only digits survive, so a number can never become some other kind of link.</summary>
public static class PhoneNumber
{
    /// <summary>
    /// "+18645551212" for a US number (10 digits, or 11 starting with 1), "+" and the digits for an international
    /// number typed with a leading +, or null if it isn't a usable number.
    /// </summary>
    public static string? Normalize(string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed) || typed.Length > 40) return null;
        var t = typed.Trim();
        bool plus = t.StartsWith('+');
        foreach (var c in t)
            if (!(char.IsAsciiDigit(c) || c is ' ' or '-' or '(' or ')' or '.' or '+')) return null;
        if (t.LastIndexOf('+') > 0) return null;
        var digits = new string(t.Where(char.IsAsciiDigit).ToArray());
        if (!plus && digits.Length == 10 && digits[0] is not ('0' or '1')) return "+1" + digits;
        if (!plus && digits.Length == 11 && digits[0] == '1' && digits[1] is not ('0' or '1')) return "+" + digits;
        if (plus && digits.Length is >= 8 and <= 15 && digits[0] != '0') return "+" + digits;
        return null;
    }

    /// <summary>"(864) 555-1212" for US numbers, otherwise "+44 ...".</summary>
    public static string Display(string normalized)
    {
        if (normalized.StartsWith("+1", StringComparison.Ordinal) && normalized.Length == 12)
        {
            var d = normalized[2..];
            return $"({d[..3]}) {d[3..6]}-{d[6..]}";
        }
        return normalized;
    }

    public static string TelLink(string normalized) => "tel:" + normalized;
    public static string SmsLink(string normalized) => "sms:" + normalized;
}

/// <summary>The one family member (or friend) Lifeline offers to contact.</summary>
public sealed class FamilyContact
{
    public const int MaxName = 40;
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";

    /// <summary>A display name: printable characters only, trimmed to 40.</summary>
    public static string CleanName(string? name)
    {
        var s = new string((name ?? "").Where(c => !char.IsControl(c) && c != '‮' && c != '‭').ToArray()).Trim();
        return s.Length > MaxName ? s[..MaxName].Trim() : s;
    }

    public bool IsSet => Name.Length > 0 && PhoneNumber.Normalize(Phone) is not null;
}

public sealed class Settings
{
    public FamilyContact Family { get; set; } = new();
    /// <summary>The welcome note explaining Ctrl+Alt+H has been shown once.</summary>
    public bool WelcomeShown { get; set; }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never };

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>Reads saved settings. A damaged, oversized or hand-edited file just gives defaults; numbers are re-checked.</summary>
    public static Settings FromJson(string? json)
    {
        var s = new Settings();
        if (string.IsNullOrWhiteSpace(json) || json.Length > 64 * 1024) return s;
        try
        {
            var read = JsonSerializer.Deserialize<Settings>(json, Json);
            if (read is null) return s;
            s.WelcomeShown = read.WelcomeShown;
            var name = FamilyContact.CleanName(read.Family?.Name);
            var phone = PhoneNumber.Normalize(read.Family?.Phone);
            if (name.Length > 0 && phone is not null) s.Family = new FamilyContact { Name = name, Phone = phone };
        }
        catch (JsonException) { }
        return s;
    }
}
