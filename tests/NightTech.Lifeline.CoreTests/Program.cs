using NightTech.Lifeline.Core;

int failures = 0, passes = 0;
void Check(bool ok, string name)
{
    if (ok) passes++; else { failures++; Console.WriteLine("FAIL: " + name); }
}

// ---------- Lists ----------
Check(Lists.RemoteToolFor("AnyDesk.exe")?.Name == "AnyDesk", "anydesk found, .exe and case ignored");
Check(Lists.RemoteToolFor("TeamViewer")?.Name == "TeamViewer", "teamviewer found");
Check(Lists.RemoteToolFor("NightTechSentinelAgent") is null, "Sentinel is never treated as a scammer tool");
Check(Lists.RemoteToolFor("notepad") is null, "notepad is not a remote tool");
Check(Lists.BrowsersFor("chrome.exe").Single().Name == "Google Chrome", "chrome browser");
Check(Lists.BrowsersFor("opera").Count() == 2, "opera and opera gx share a process name");
Check(!Lists.BrowsersFor("explorer").Any(), "explorer is not a browser");
Check(Lists.Normalize("  MSEDGE.EXE ") == "msedge", "normalize");
Check(Lists.Browsers.All(b => b.ProcessNames.All(p => p == p.ToLowerInvariant())), "browser process names lower case");
Check(Lists.RemoteTools.All(t => t.ProcessNames.All(p => p == p.ToLowerInvariant())), "remote tool process names lower case");
Check(Lists.RemoteTools.SelectMany(t => t.ProcessNames).Distinct().Count() == Lists.RemoteTools.Sum(t => t.ProcessNames.Length), "no process name in two remote tools");
var browserProcs = Lists.Browsers.SelectMany(b => b.ProcessNames).ToHashSet();
Check(!Lists.RemoteTools.SelectMany(t => t.ProcessNames).Any(browserProcs.Contains), "no process is both a browser and a remote tool");
var never = new[] { "explorer", "svchost", "csrss", "winlogon", "lsass", "dwm", "nighttechlifeline", "nighttechshield", "nighttechsentinel" };
Check(!never.Any(n => browserProcs.Contains(n) || Lists.RemoteToolFor(n) is not null), "system and Night Tech processes are never closed");

// ---------- Session files ----------
var fs = new FakeFs();
const string L = @"C:\Users\pat\AppData\Local", R = @"C:\Users\pat\AppData\Roaming", B = @"C:\Users\pat\AppData\Local\Night Tech Solutions\Lifeline\Saved tabs\2026-11-05 143012";
var chrome = Lists.Browsers.First(b => b.Name == "Google Chrome");
fs.Dir(L + @"\Google\Chrome\User Data\Default\Sessions");
fs.File(L + @"\Google\Chrome\User Data\Default\Current Session");
fs.File(L + @"\Google\Chrome\User Data\Default\Last Tabs");
fs.Dir(L + @"\Google\Chrome\User Data\Profile 2\Sessions");
fs.Dir(L + @"\Google\Chrome\User Data\System Profile\Sessions");      // not a real profile
fs.Dir(L + @"\Google\Chrome\User Data\Crashpad");
fs.Dir(L + @"\Google\Chrome\User Data\Profile X\Sessions");           // not a numbered profile
var items = SessionFiles.Find(chrome, L, R, B, fs);
Check(items.Count == 4, "chrome: 4 items (" + string.Join("; ", items.Select(i => i.Source)) + ")");
Check(items.Any(i => i.IsDirectory && i.Source == L + @"\Google\Chrome\User Data\Default\Sessions" && i.Destination == B + @"\Google Chrome\Default\Sessions"), "chrome default sessions folder and destination");
Check(items.Any(i => !i.IsDirectory && i.Source.EndsWith(@"Default\Last Tabs")), "chrome legacy last tabs file");
Check(items.Any(i => i.Source == L + @"\Google\Chrome\User Data\Profile 2\Sessions"), "chrome profile 2");
Check(!items.Any(i => i.Source.Contains("System Profile") || i.Source.Contains("Profile X") || i.Source.Contains("Crashpad")), "chrome ignores non-profile folders");
Check(items.All(i => i.Browser == "Google Chrome"), "items tagged with browser");

fs.Reparse(L + @"\Google\Chrome\User Data\Profile 2");
Check(SessionFiles.Find(chrome, L, R, B, fs).All(i => !i.Source.Contains("Profile 2")), "linked profile folder is skipped");
fs.Reparse(L + @"\Google\Chrome\User Data\Default\Sessions");
Check(SessionFiles.Find(chrome, L, R, B, fs).All(i => !i.Source.EndsWith(@"Default\Sessions")), "linked sessions folder is skipped");
fs.Reparse(L + @"\Google\Chrome\User Data");
Check(SessionFiles.Find(chrome, L, R, B, fs).Count == 0, "linked data folder: nothing");

var firefox = Lists.Browsers.First(b => b.Name == "Firefox");
fs.File(R + @"\Mozilla\Firefox\Profiles\abcd.default-release\sessionstore.jsonlz4");
fs.Dir(R + @"\Mozilla\Firefox\Profiles\abcd.default-release\sessionstore-backups");
fs.Dir(R + @"\Mozilla\Firefox\Profiles\efgh.other");
var ff = SessionFiles.Find(firefox, L, R, B, fs);
Check(ff.Count == 2, "firefox: 2 items");
Check(ff.Any(i => i.Destination == B + @"\Firefox\abcd.default-release\sessionstore.jsonlz4"), "firefox destination");

var opera = Lists.Browsers.First(b => b.Name == "Opera");
fs.Dir(R + @"\Opera Software\Opera Stable\Sessions");
var op = SessionFiles.Find(opera, L, R, B, fs);
Check(op.Count == 1 && op[0].Destination == B + @"\Opera\Sessions", "opera profile is the data folder");
Check(SessionFiles.Find(Lists.Browsers.First(b => b.Name == "Microsoft Edge"), L, R, B, fs).Count == 0, "edge not installed: nothing");
Check(SessionFiles.Find(Lists.Browsers.First(b => b.Kind == BrowserKind.NoSession), L, R, B, fs).Count == 0, "internet explorer: nothing to move");

Check(SessionFiles.IsChromiumProfile("Default") && SessionFiles.IsChromiumProfile("Profile 12") && SessionFiles.IsChromiumProfile("Guest Profile"), "chromium profile names");
Check(!SessionFiles.IsChromiumProfile("Profile ") && !SessionFiles.IsChromiumProfile("Profile 1a") && !SessionFiles.IsChromiumProfile("System Profile"), "not chromium profiles");
Check(SessionFiles.SafeName(@"..\evil") == ".._evil" && SessionFiles.SafeName("a/b") == "a_b", "safe name has no separator");
Check(SessionFiles.SafeName("..") == "_" && SessionFiles.SafeName("") == "_" && SessionFiles.SafeName("a:b") == "a_b", "safe name edge cases");
Check(SessionFiles.SafeName(new string('x', 100)).Length == 60, "safe name length");
Check(SessionFiles.StampFolder(new DateTime(2026, 11, 5, 14, 30, 12)) == "2026-11-05 143012", "stamp folder");

// ---------- Phone numbers ----------
Check(PhoneNumber.Normalize("(864) 555-1212") == "+18645551212", "us number");
Check(PhoneNumber.Normalize("1-864-555-1212") == "+18645551212", "us number with 1");
Check(PhoneNumber.Normalize("864.555.1212") == "+18645551212", "dots");
Check(PhoneNumber.Normalize("+44 20 7946 0958") == "+442079460958", "international");
Check(PhoneNumber.Normalize("555-1212") is null, "too short");
Check(PhoneNumber.Normalize("123-456-7890") is null, "area code cannot start with 1");
Check(PhoneNumber.Normalize("864-555-1212?x=1") is null, "query text refused");
Check(PhoneNumber.Normalize("javascript:alert(1)") is null, "script refused");
Check(PhoneNumber.Normalize("86+4555 1212") is null, "plus in the middle refused");
Check(PhoneNumber.Normalize("") is null && PhoneNumber.Normalize(null) is null, "empty");
Check(PhoneNumber.Normalize(new string('1', 50)) is null, "very long refused");
Check(PhoneNumber.Display("+18645551212") == "(864) 555-1212", "display us");
Check(PhoneNumber.Display("+442079460958") == "+442079460958", "display international");
Check(PhoneNumber.TelLink("+18645551212") == "tel:+18645551212" && PhoneNumber.SmsLink("+18645551212") == "sms:+18645551212", "links");

// ---------- Settings ----------
var st = new Settings { WelcomeShown = true, Family = new FamilyContact { Name = "Amy", Phone = "+18645551212" } };
var back = Settings.FromJson(st.ToJson());
Check(back.WelcomeShown && back.Family.Name == "Amy" && back.Family.Phone == "+18645551212" && back.Family.IsSet, "settings round trip");
Check(!Settings.FromJson("{not json").Family.IsSet, "bad json gives defaults");
Check(!Settings.FromJson(null).WelcomeShown, "null json");
Check(!Settings.FromJson("{\"Family\":{\"Name\":\"Scam\",\"Phone\":\"tel:1900\"}}").Family.IsSet, "bad saved phone dropped");
Check(Settings.FromJson("{\"Family\":{\"Name\":\"Amy\\u202E\\u0007\",\"Phone\":\"864 555 1212\"}}").Family.Name == "Amy", "saved name cleaned");
Check(!Settings.FromJson(new string(' ', 70000) + "{}").WelcomeShown, "oversized file ignored");
Check(FamilyContact.CleanName(new string('a', 80)).Length == 40, "name trimmed to 40");
Check(!new FamilyContact { Name = "", Phone = "+18645551212" }.IsSet, "contact needs a name");

// ---------- Words ----------
Check(Words.JoinNames(new[] { "A" }) == "A" && Words.JoinNames(new[] { "A", "B" }) == "A and B" && Words.JoinNames(new[] { "A", "B", "C" }) == "A, B and C", "join names");
Check(Words.JoinNames(Array.Empty<string>()) == "", "join none");
var rr = new RescueResult { When = new DateTime(2026, 11, 5, 14, 30, 12) };
Check(Words.RescueSummary(rr).StartsWith("No web browsers were open"), "nothing open summary");
rr.BrowsersClosed["Google Chrome"] = 12;
rr.TabsSaved.Add(new MoveItem("Google Chrome", "a", "b", true));
var sum = Words.RescueSummary(rr);
Check(sum.Contains("closed Google Chrome") && sum.Contains("won't come back"), "rescue summary");
rr.BrowsersStillOpen.Add("Microsoft Edge");
Check(Words.RescueSummary(rr).Contains("Microsoft Edge would not close") && !Words.RescueSummary(rr).Contains("won't come back"), "still-open summary");
rr.TabsNotMoved.Add("x");
Check(Words.RescueSummary(rr).Contains("press this button again"), "not-moved summary");
Check(Words.LogLine(rr).Contains("Google Chrome x12") && Words.LogLine(rr).Contains("2026-11-05 14:30:12"), "rescue log line");
var co = new CutOffResult { When = rr.When, WifiTurnedOff = true };
Check(Words.CutOffSummary(co).Contains("didn't find any") && Words.CutOffSummary(co).Contains("Wi-Fi is off"), "cut-off nothing found");
co.RemoteToolsClosed.Add("AnyDesk"); co.RemoteToolsStillRunning.Add("TeamViewer"); co.WifiTurnedOff = false; co.WifiNote = "no Wi-Fi";
var cs = Words.CutOffSummary(co);
Check(cs.Contains("closed AnyDesk") && cs.Contains("TeamViewer is still running") && cs.Contains("couldn't turn off Wi-Fi (no Wi-Fi)"), "cut-off summary");
Check(Words.LogLine(co).Contains("CUTOFF closed=[AnyDesk]"), "cut-off log line");

// ---------- Brand ----------
Check(Brand.AllowedLinks.All(l => l.StartsWith("https://", StringComparison.Ordinal) || l.StartsWith("tel:+1", StringComparison.Ordinal) || l.StartsWith("sms:+1", StringComparison.Ordinal) || l.StartsWith("mailto:", StringComparison.Ordinal)), "allowed links are safe kinds");
Check(Brand.AllowedLinks.Contains(Brand.RemoteSupport) && Brand.AllowedLinks.Contains(Brand.PhoneLink), "allowed links include help");

Console.WriteLine($"{passes} passed, {failures} failed");
return failures == 0 ? 0 : 1;

/// <summary>An in-memory disk with Windows-style paths.</summary>
sealed class FakeFs : IFileSystem
{
    private readonly HashSet<string> _dirs = new(StringComparer.OrdinalIgnoreCase), _files = new(StringComparer.OrdinalIgnoreCase), _links = new(StringComparer.OrdinalIgnoreCase);
    public void Dir(string p) { _dirs.Add(p); AddParents(p); }
    public void File(string p) { _files.Add(p); AddParents(p); }
    public void Reparse(string p) => _links.Add(p);
    private void AddParents(string p) { for (int i = p.LastIndexOf('\\'); i > 2; i = p.LastIndexOf('\\', i - 1)) _dirs.Add(p[..i]); }
    public bool DirectoryExists(string path) => _dirs.Contains(path);
    public bool FileExists(string path) => _files.Contains(path);
    public IEnumerable<string> GetDirectories(string path) => _dirs.Where(d => d.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase) && d.IndexOf('\\', path.Length + 1) < 0).ToList();
    public bool IsReparsePoint(string path) => _links.Contains(path);
    public string Combine(string a, string b) => a.TrimEnd('\\') + "\\" + b;
    public string GetFileName(string path) => path[(path.LastIndexOf('\\') + 1)..];
}
