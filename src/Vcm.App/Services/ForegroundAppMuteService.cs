using System.Diagnostics;
using System.Runtime.InteropServices;
using Vcm.App.Native;
using Vcm.Core;

namespace Vcm.App.Services;

/// <summary>
/// Toggles the microphone in the foreground meeting application by sending its own mute shortcut, so the
/// application's mute state follows the one of VCM.
/// </summary>
internal static class ForegroundAppMuteService
{
    private static readonly TimeSpan ModifierReleaseTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Resolves the foreground window now and sends the shortcut in the background.</summary>
    public static void Toggle()
    {
        IntPtr window = NativeMethods.GetForegroundWindow();
        if (window == IntPtr.Zero) return;

        var shortcut = FindShortcut(window);
        if (shortcut is null) return;

        Task.Run(() => Send(window, shortcut)).ContinueWith(
            t => Logger.Error($"Sending the application mute shortcut failed: {t.Exception?.GetBaseException().Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private static HotkeySettings? FindShortcut(IntPtr window)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(window, out uint pid);
            using var process = Process.GetProcessById((int)pid);
            var title = new char[512];
            int length = NativeMethods.GetWindowText(window, title, title.Length);
            return AppMuteShortcuts.Find(process.ProcessName, new string(title, 0, Math.Max(length, 0)));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static void Send(IntPtr window, HotkeySettings shortcut)
    {
        // A modifier still held from VCM's own shortcut would combine with the injected keys.
        var deadline = DateTime.UtcNow + ModifierReleaseTimeout;
        while (AnyModifierDown())
        {
            if (DateTime.UtcNow > deadline)
            {
                Logger.Warn("Application mute shortcut skipped: modifiers are still held");
                return;
            }
            Thread.Sleep(10);
        }

        if (NativeMethods.GetForegroundWindow() != window) return;

        var keys = new List<int>();
        if (shortcut.Win) keys.Add(KeyNames.VK_LWIN);
        if (shortcut.Ctrl) keys.Add(KeyNames.VK_CONTROL);
        if (shortcut.Alt) keys.Add(KeyNames.VK_MENU);
        if (shortcut.Shift) keys.Add(KeyNames.VK_SHIFT);
        keys.Add(shortcut.Code);

        var inputs = new List<NativeMethods.INPUT>();
        foreach (int vk in keys) inputs.Add(Key(vk, up: false));
        foreach (int vk in Enumerable.Reverse(keys)) inputs.Add(Key(vk, up: true));

        uint sent = NativeMethods.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != inputs.Count) Logger.Warn($"SendInput sent {sent} of {inputs.Count} events");
    }

    private static bool AnyModifierDown() =>
        IsDown(KeyNames.VK_LWIN) || IsDown(KeyNames.VK_RWIN) || IsDown(KeyNames.VK_SHIFT)
        || IsDown(KeyNames.VK_CONTROL) || IsDown(KeyNames.VK_MENU);

    private static bool IsDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

    private static NativeMethods.INPUT Key(int vk, bool up) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = (ushort)vk,
                dwFlags = up ? NativeMethods.KEYEVENTF_KEYUP : 0,
                dwExtraInfo = KeyboardHook.InjectedMarker,
            },
        },
    };
}
