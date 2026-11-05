using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using NightTech.Lifeline.Core;
using NightTech.Lifeline.Windows;
using Forms = System.Windows.Forms;

namespace NightTech.Lifeline;

/// <summary>
/// Lifeline lives as an icon by the clock, starts with Windows (the installer adds "--background"), and opens its
/// window from the icon, the desktop shortcut or Ctrl+Alt+H. Only one copy runs; opening it again shows that copy.
/// </summary>
public partial class App : Application
{
    /// <summary>Matches AppMutex in the installer, so setup asks to close Lifeline before upgrading.</summary>
    public const string MutexName = "NightTechSolutions.Lifeline";
    private const string ShowEventName = "NightTechSolutions.Lifeline.Show";

    private Mutex? _single;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showWait;
    private Forms.NotifyIcon? _tray;
    private Hotkey? _hotkey;
    private MainWindow? _window;

    public string HotkeyText => _hotkey?.Registered == true ? _hotkey.Text : "";
    public Settings Settings { get; private set; } = new();

    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int processId);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // One unexpected error must never quietly take the icon and the shortcut away.
        DispatcherUnhandledException += (_, a) => { Store.Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {a.Exception.GetType().Name}: {a.Exception.Message}"); a.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Store.Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} FATAL {a.ExceptionObject}");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, a) => { Store.Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} TASK {a.Exception.Message}"); a.SetObserved(); };
        bool background = e.Args.Any(a => a.Equals("--background", StringComparison.OrdinalIgnoreCase));

        // The show-window event is made before the one-copy mutex, so a second copy started a moment later always finds it.
        bool first;
        try
        {
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName, out bool createdEvent);
            _single = new Mutex(true, MutexName, out first);
            if (!first && !createdEvent)
            {
                // Already running: ask that copy to show its window, then leave.
                if (!background) { AllowSetForegroundWindow(-1); _showEvent.Set(); }
                _showEvent.Dispose();
                _showEvent = null;
                Shutdown();
                return;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or WaitHandleCannotBeOpenedException or IOException)
        {
            // A copy started as administrator holds the names. Carry on without them rather than leave the person with nothing.
            _showEvent?.Dispose();
            _showEvent = null;
        }
        if (_showEvent is not null)
            _showWait = ThreadPool.RegisterWaitForSingleObject(_showEvent, (_, _) => Dispatcher.BeginInvoke(() => ShowWindow()), null, Timeout.Infinite, false);

        Settings = Store.LoadSettings();
        _hotkey = new Hotkey(() => ShowWindow());
        _tray = MakeTray();

        if (!background) ShowWindow();
        if (!Settings.WelcomeShown)
        {
            var keys = HotkeyText.Length > 0 ? $"Press {HotkeyText} any time" : "Double-click this icon";
            _tray.ShowBalloonTip(15000, "Night Tech Lifeline is ready", $"{keys} if a scary message takes over your screen. Lifeline will help you get out of it.", Forms.ToolTipIcon.Info);
            Settings.WelcomeShown = true;
            Store.SaveSettings(Settings);
        }
    }

    public void SaveSettings(Settings s)
    {
        Settings = s;
        Store.SaveSettings(s);
        _window?.RefreshFamily();
    }

    public void ShowWindow(bool rescueNow = false)
    {
        _window ??= new MainWindow();
        _window.Bring();
        if (rescueNow) _window.StartRescue();
    }

    private Forms.NotifyIcon MakeTray()
    {
        var menu = new Forms.ContextMenuStrip { Font = new System.Drawing.Font("Segoe UI", 11f) };
        var keys = HotkeyText.Length > 0 ? $"   ({HotkeyText})" : "";
        menu.Items.Add("Open Lifeline" + keys, null, (_, _) => ShowWindow());
        menu.Items.Add(Words.RescueButton, null, (_, _) => ShowWindow(rescueNow: true));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add($"Call Night Tech: {Brand.Phone}", null, (_, _) => { ShowWindow(); _window?.CallNightTech(); });
        menu.Items.Add("Settings", null, (_, _) => { ShowWindow(); _window?.OpenSettings(); });
        menu.Items.Add("About Lifeline", null, (_, _) => { ShowWindow(); _window?.OpenAbout(); });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Close Lifeline", null, (_, _) => AskToQuit());

        System.Drawing.Icon icon;
        var res = GetResourceStream(new Uri("pack://application:,,,/Assets/lifeline.ico"));
        using (var s = res.Stream) icon = new System.Drawing.Icon(s, Forms.SystemInformation.SmallIconSize);

        var tray = new Forms.NotifyIcon { Icon = icon, Text = "Night Tech Lifeline: scam help", ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += (_, a) => { if (a.Button == Forms.MouseButtons.Left) ShowWindow(); };
        tray.BalloonTipClicked += (_, _) => ShowWindow();
        return tray;
    }

    private void AskToQuit()
    {
        var keys = HotkeyText.Length > 0 ? $" and {HotkeyText} won't work" : "";
        if (MessageBox.Show($"If you close Lifeline, its icon goes away{keys} until you open Lifeline again.\n\nClose Lifeline?",
                "Night Tech Lifeline", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showWait?.Unregister(null);
        _showEvent?.Dispose();
        _hotkey?.Dispose();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        _single?.Dispose();
        base.OnExit(e);
    }
}
