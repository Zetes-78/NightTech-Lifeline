using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NightTech.Lifeline.Core;
using NightTech.Lifeline.Windows;

namespace NightTech.Lifeline;

public partial class MainWindow : Window
{
    private static App App => (App)Application.Current;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        RescueTitle.Text = Words.RescueButton;
        RescueSub.Text = Words.RescueButtonSub;
        CutOffTitle.Text = Words.CutOffButton;
        CutOffSub.Text = Words.CutOffButtonSub;
        CallButton.Content = $"Call Night Tech:  {Brand.Phone}";
        TextButton.Content = $"Text Night Tech:  {Brand.TextPhone}";
        HoursText.Text = Brand.Hours + ". A real person from our family business will answer or call you back.";
        Fill(FactsList, Words.Facts);
        Fill(NeverList, Words.NeverDo);
        Fill(ActedList, Words.IfAlreadyActed);
        HotkeyNote.Text = App.HotkeyText.Length > 0
            ? $"Press {App.HotkeyText} any time to open Lifeline. If the keys don't work, click the round Lifeline icon by the clock."
            : "Lifeline's shortcut keys are being used by another program. Use the Lifeline icon by the clock instead.";
        RefreshFamily();
        MaxHeight = SystemParameters.WorkArea.Height;
        if (Height > MaxHeight) Height = MaxHeight;
    }

    private static void Fill(ItemsControl list, string[] lines)
    {
        foreach (var l in lines)
            list.Items.Add(new TextBlock { Text = "•  " + l, TextWrapping = TextWrapping.Wrap, FontSize = 18, Margin = new Thickness(0, 0, 0, 6) });
    }

    public void RefreshFamily()
    {
        var f = App.Settings.Family;
        FamilyButton.Content = f.IsSet
            ? $"Call {f.Name}:  {PhoneNumber.Display(f.Phone)}" + (f.Added is DateTime d ? $"\n(added {d:MMMM d, yyyy})" : "")
            : "Add a family member or friend to call";
    }

    /// <summary>Shows the window on top of everything, including a full-screen browser.</summary>
    public void Bring()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Topmost = true;
        Activate();
        Focus();
        RescueButton.Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closing the window keeps Lifeline running by the clock. "Close Lifeline" in the icon's menu quits.
        e.Cancel = true;
        Topmost = false;
        Hide();
    }

    // ---------- Emergency buttons ----------

    private void Rescue_Click(object sender, RoutedEventArgs e) => StartRescue();

    public async void StartRescue()
    {
        if (_busy) return;
        _busy = true;
        if (MessageBox.Show(this, Words.ConfirmRescue, "Get me out of this scam screen", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes)
        {
            _busy = false;
            return;
        }
        SetBusy(true, "Closing your web browsers...");
        try
        {
            var r = await Task.Run(Rescuer.Run);
            ShowResult(r.ClosedAnything && r.BrowsersStillOpen.Count == 0 ? "The scam screen is closed" : "Here's what happened", Words.RescueSummary(r), wifiButton: false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ShowResult("Something went wrong", $"Lifeline couldn't close everything. Hold the power button until the computer turns off, then call Night Tech at {Brand.Phone}.", false);
        }
        finally { SetBusy(false, null); }
    }

    private async void CutOff_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        if (MessageBox.Show(this, Words.ConfirmCutOff, "Someone is on my computer", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) != MessageBoxResult.Yes)
        {
            _busy = false;
            return;
        }
        SetBusy(true, "Cutting off remote access...");
        try
        {
            var r = await CutOff.RunAsync();
            ShowResult(r.SurelyCutOff ? "Remote access is cut off" : "Unplug the internet now", Words.CutOffSummary(r), wifiButton: r.WifiTurnedOff == true);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ShowResult("Unplug the internet now", $"Lifeline couldn't finish. Unplug the internet cable or turn off the computer, then call Night Tech at {Brand.Phone}.", false);
        }
        finally { SetBusy(false, null); }
    }

    private async void WifiOn_Click(object sender, RoutedEventArgs e)
    {
        WifiOnButton.IsEnabled = false;
        bool ok;
        try { ok = await CutOff.WifiOnAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { ok = false; }
        WifiOnButton.Content = ok ? "Wi-Fi is back on" : "Couldn't turn Wi-Fi on. Use the Wi-Fi button by the clock.";
    }

    private void SetBusy(bool busy, string? message)
    {
        _busy = busy;
        RescueButton.IsEnabled = CutOffButton.IsEnabled = !busy;
        if (busy && message is not null) ShowResult("Please wait", message, false);
        Cursor = busy ? System.Windows.Input.Cursors.Wait : null;
    }

    private void ShowResult(string title, string text, bool wifiButton)
    {
        ResultTitle.Text = title;
        ResultText.Text = text;
        WifiOnButton.Visibility = wifiButton ? Visibility.Visible : Visibility.Collapsed;
        WifiOnButton.IsEnabled = true;
        WifiOnButton.Content = "Turn Wi-Fi back on (only when Night Tech says it's safe)";
        ResultBox.Visibility = Visibility.Visible;
        ResultBox.BringIntoView();
    }

    // ---------- People ----------

    private void Call_Click(object sender, RoutedEventArgs e) => CallNightTech();

    public void CallNightTech() => Dial(Brand.PhoneLink, "tel", $"Call Night Tech from your phone:\n\n{Brand.Phone}\n\n{Brand.Hours}.");

    private void Text_Click(object sender, RoutedEventArgs e) =>
        Dial(Brand.TextLink, "sms", $"Text Night Tech from your phone:\n\n{Brand.TextPhone}");

    private void Family_Click(object sender, RoutedEventArgs e)
    {
        var f = App.Settings.Family;
        if (!f.IsSet) { OpenSettings(); return; }
        Dial(PhoneNumber.TelLink(f.Phone), "tel", $"Call {f.Name} from your phone:\n\n{PhoneNumber.Display(f.Phone)}");
    }

    private void Remote_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, $"Call Night Tech at {Brand.Phone} first. We'll tell you exactly what to click, and you'll see everything we do.\n\nNever let anyone else connect to your computer.\n\nChoose OK to open Night Tech's remote help page.",
            "Let Night Tech help me", MessageBoxButton.OK, MessageBoxImage.Information);
        Topmost = false;
        Links.Open(Brand.RemoteSupport);
    }

    /// <summary>Opens the phone app if Windows has one (Phone Link and similar); otherwise shows the number to dial.</summary>
    private void Dial(string link, string scheme, string fallback)
    {
        if (Links.CanOpen(scheme) && Links.Open(link)) return;
        MessageBox.Show(this, fallback, "Night Tech Lifeline", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ---------- Settings and About ----------

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void About_Click(object sender, RoutedEventArgs e) => OpenAbout();

    public void OpenSettings()
    {
        var current = App.Settings.Family;
        var win = Dialog("Lifeline settings");
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Family member or friend to call", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(new TextBlock { Text = "Lifeline shows a big button to call this person. Only add someone you know personally. Never add a number a caller or a pop-up gives you. It stays on this computer and is never sent anywhere.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) });
        panel.Children.Add(new TextBlock { Text = "Name" });
        var name = new TextBox { Text = current.Name, MaxLength = FamilyContact.MaxName, FontSize = 18, Padding = new Thickness(6), Margin = new Thickness(0, 4, 0, 12) };
        panel.Children.Add(name);
        panel.Children.Add(new TextBlock { Text = "Phone number" });
        var phone = new TextBox { Text = current.IsSet ? PhoneNumber.Display(current.Phone) : "", MaxLength = 30, FontSize = 18, Padding = new Thickness(6), Margin = new Thickness(0, 4, 0, 6) };
        panel.Children.Add(phone);
        var error = new TextBlock { Foreground = (Brush)FindResource("OrangeDark"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(error);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var save = new Button { Content = "Save", Style = (Style)FindResource("Small"), IsDefault = true };
        var clear = new Button { Content = "Remove", Style = (Style)FindResource("Small") };
        var cancel = new Button { Content = "Cancel", Style = (Style)FindResource("Small"), IsCancel = true };
        save.Click += (_, _) =>
        {
            var n = FamilyContact.CleanName(name.Text);
            var p = PhoneNumber.Normalize(phone.Text);
            if (n.Length == 0) { error.Text = "Type a name."; return; }
            if (p is null) { error.Text = "That phone number doesn't look right. Type it like (864) 555-1212."; return; }
            if (FamilyContact.LooksOfficial(n))
            {
                error.Text = "This button is only for someone you know personally, like a son, daughter or friend. Companies, banks and \"support\" lines don't go here. If someone asked you to add their number, that's a scam.";
                return;
            }
            var s = App.Settings;
            s.Family = new FamilyContact { Name = n, Phone = p, Added = DateTime.Now };
            App.SaveSettings(s);
            Store.Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} FAMILY set name=\"{n}\" phone={p}");
            win.Close();
        };
        clear.Click += (_, _) => { var s = App.Settings; s.Family = new FamilyContact(); App.SaveSettings(s); Store.Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} FAMILY removed"); win.Close(); };
        buttons.Children.Add(save);
        if (current.IsSet) buttons.Children.Add(clear);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        panel.Children.Add(new TextBlock { Text = "For Night Tech technicians", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 26, 0, 4) });
        panel.Children.Add(new TextBlock { Text = "Browser tabs Lifeline closed are kept here, one folder per rescue.", TextWrapping = TextWrapping.Wrap, FontSize = 15 });
        var folder = new Button { Content = "Open the saved tabs folder", Style = (Style)FindResource("Small"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
        folder.Click += (_, _) => Links.OpenFolder(Store.SavedTabsFolder);
        panel.Children.Add(folder);

        win.Content = panel;
        win.ShowDialog();
    }

    public void OpenAbout()
    {
        var v = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "";
        var win = Dialog("About Night Tech Lifeline");
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/nighttech-logo.png")), Height = 90, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) });
        panel.Children.Add(new TextBlock { Text = $"Night Tech Lifeline {v}", FontSize = 20, FontWeight = FontWeights.Bold });
        panel.Children.Add(new TextBlock
        {
            Text = "Free from Night Tech Solutions, a family-owned IT business in the Upstate of South Carolina. No ads, no account, and nothing is ever sent over the internet. " +
                   "Lifeline only closes your own programs and moves your browser's saved tabs into its own folder; it never deletes anything.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12),
        });
        panel.Children.Add(new TextBlock { Text = $"Call {Brand.Phone}   ·   Text {Brand.TextPhone}\n{Brand.Email}\n{Brand.Hours}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        var links = new WrapPanel();
        foreach (var (label, url) in new[] { ("nighttech.solutions", Brand.Website), ("Privacy", Brand.Privacy), ("Source code", Brand.Source) })
        {
            var b = new Button { Content = label, Style = (Style)FindResource("Small"), Margin = new Thickness(0, 0, 10, 8) };
            b.Click += (_, _) => Links.Open(url);
            links.Children.Add(b);
        }
        panel.Children.Add(links);
        panel.Children.Add(new TextBlock { Text = Brand.Verse, FontStyle = FontStyles.Italic, Foreground = (Brush)FindResource("Muted"), FontSize = 15, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) });
        var ok = new Button { Content = "OK", Style = (Style)FindResource("Small"), IsDefault = true, IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        ok.Click += (_, _) => win.Close();
        panel.Children.Add(ok);
        win.Content = panel;
        win.ShowDialog();
    }

    private Window Dialog(string title) => new()
    {
        Title = title, Owner = this, Width = 560, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
        WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new FontFamily("Segoe UI"), FontSize = 17,
        Topmost = Topmost, ShowInTaskbar = false,
    };
}
