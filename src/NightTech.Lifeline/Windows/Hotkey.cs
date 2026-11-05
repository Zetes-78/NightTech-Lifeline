using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace NightTech.Lifeline.Windows;

/// <summary>
/// The Ctrl+Alt+H shortcut. It works even while a fake warning fills the screen, because Windows handles it before
/// the browser sees the keys. If another program already uses Ctrl+Alt+H, Ctrl+Alt+L is used instead.
/// </summary>
public sealed class Hotkey : IDisposable
{
    private const int WM_HOTKEY = 0x0312, Id = 0x4E54;
    private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_NOREPEAT = 0x4000, VK_H = 0x48, VK_L = 0x4C;
    private readonly HwndSource _source;
    private readonly Action _pressed;
    public string Text { get; } = "";
    public bool Registered { get; }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public Hotkey(Action pressed)
    {
        _pressed = pressed;
        // A message-only window (parent HWND_MESSAGE) just to receive the shortcut.
        _source = new HwndSource(new HwndSourceParameters("NightTechLifelineHotkey") { ParentWindow = new IntPtr(-3), WindowStyle = 0 });
        _source.AddHook(Hook);
        if (RegisterHotKey(_source.Handle, Id, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_H)) { Registered = true; Text = "Ctrl + Alt + H"; }
        else if (RegisterHotKey(_source.Handle, Id, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_L)) { Registered = true; Text = "Ctrl + Alt + L"; }
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == Id) { handled = true; _pressed(); }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (Registered) UnregisterHotKey(_source.Handle, Id);
        _source.RemoveHook(Hook);
        _source.Dispose();
    }
}
