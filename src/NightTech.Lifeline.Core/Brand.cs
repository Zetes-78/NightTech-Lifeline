namespace NightTech.Lifeline.Core;

/// <summary>Night Tech Solutions' details, shown in the app. Every link Lifeline opens comes from here, never from a file.</summary>
public static class Brand
{
    public const string Company = "Night Tech Solutions";
    public const string Website = "https://nighttech.solutions/";
    public const string LifelinePage = "https://nighttech.solutions/security-apps/#lifeline";
    public const string RemoteSupport = "https://nighttech.solutions/remote-support/";
    public const string ScamCheck = "https://nighttech.solutions/scam-check/";
    public const string Privacy = "https://nighttech.solutions/privacy/#apps";
    public const string Source = "https://github.com/Zetes-78/NightTech-Lifeline";
    public const string Phone = "(864) 381-8510";
    public const string PhoneLink = "tel:+18643818510";
    public const string TextPhone = "(864) 497-2701";
    public const string TextLink = "sms:+18644972701";
    public const string Email = "nighttechsolutions1@gmail.com";
    public const string Hours = "Weeknights 6-11 PM, weekends 9 AM-6 PM, remote help during the day";
    public const string Verse = "“Whatever you do, work heartily, as for the Lord.” Colossians 3:23";
    public const string Navy = "#1A365D", Orange = "#FF6B35", OrangeText = "#C2410C", Blue = "#4299E1", Gray = "#F7FAFC";

    /// <summary>The only links Lifeline will ever open.</summary>
    public static readonly IReadOnlySet<string> AllowedLinks = new HashSet<string>(StringComparer.Ordinal)
    {
        Website, LifelinePage, RemoteSupport, ScamCheck, Privacy, Source, PhoneLink, TextLink, "mailto:" + Email,
    };
}
