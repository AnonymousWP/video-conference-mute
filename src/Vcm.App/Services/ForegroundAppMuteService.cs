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
    private static readonly object SendGate = new();

    // Modifiers that may still be physically held from VCM's own shortcut.
    private static readonly int[] ModifierKeys =
    [
        KeyNames.VK_LWIN, KeyNames.VK_RWIN, KeyNames.VK_LSHIFT, KeyNames.VK_RSHIFT,
        KeyNames.VK_LCONTROL, KeyNames.VK_RCONTROL, KeyNames.VK_LMENU, KeyNames.VK_RMENU,
    ];

    /// <summary>Resolves the foreground window now and sends the shortcut in the background.</summary>
    public static void Toggle()
    {
        IntPtr window = NativeMethods.GetForegroundWindow();
        if (window == IntPtr.Zero)
        {
            Logger.Info("Application mute: no foreground window");
            return;
        }

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
            string text = new(title, 0, Math.Max(length, 0));
            var shortcut = AppMuteShortcuts.Find(process.ProcessName, text);
            Logger.Info($"Application mute: foreground {process.ProcessName} \"{text}\" -> {shortcut?.ToString() ?? "no match"}");
            return shortcut;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Logger.Warn($"Application mute: cannot inspect the foreground window: {ex.Message}");
            return null;
        }
    }

    private static void Send(IntPtr window, HotkeySettings shortcut)
    {
        lock (SendGate)
        {
            if (NativeMethods.GetForegroundWindow() != window)
            {
                Logger.Info("Application mute shortcut skipped: foreground window changed");
                return;
            }

            var held = ModifierKeys.Where(IsDown).ToList();
            var array = AppMuteKeySequence.Build(shortcut, held).Select(k => Key(k.Vk, k.Up)).ToArray();
            string heldNames = string.Join(", ", held.Select(KeyNames.GetName));
            uint sent = NativeMethods.SendInput((uint)array.Length, array, Marshal.SizeOf<NativeMethods.INPUT>());
            if (sent != array.Length) Logger.Warn($"SendInput sent {sent} of {array.Length} events (error {Marshal.GetLastWin32Error()})");
            else Logger.Info($"Application mute shortcut {shortcut} sent, held modifiers: {heldNames}");
        }
    }

    private static bool IsDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

    private static bool IsExtended(int vk) => vk is KeyNames.VK_LWIN or KeyNames.VK_RWIN or KeyNames.VK_RCONTROL or KeyNames.VK_RMENU;

    // Browsers derive KeyboardEvent.code from the scan code, so it must be set along with the virtual key.
    private static NativeMethods.INPUT Key(int vk, bool up) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = (ushort)vk,
                wScan = (ushort)NativeMethods.MapVirtualKey((uint)vk, 0 /* MAPVK_VK_TO_VSC */),
                dwFlags = (up ? NativeMethods.KEYEVENTF_KEYUP : 0) | (IsExtended(vk) ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0),
                dwExtraInfo = KeyboardHook.InjectedMarker,
            },
        },
    };
}
