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

    // Modifiers that may still be physically held from VCM's own shortcut; the flag marks extended keys.
    private static readonly (int Vk, bool Extended)[] ModifierKeys =
    [
        (KeyNames.VK_LWIN, true), (KeyNames.VK_RWIN, true),
        (KeyNames.VK_LSHIFT, false), (KeyNames.VK_RSHIFT, false),
        (KeyNames.VK_LCONTROL, false), (KeyNames.VK_RCONTROL, true),
        (KeyNames.VK_LMENU, false), (KeyNames.VK_RMENU, true),
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

            // Modifiers still held from VCM's own shortcut would combine with the key, so they are lifted for the
            // duration of one atomic batch and pressed again afterwards.
            var held = ModifierKeys.Where(k => IsDown(k.Vk)).ToList();
            var inputs = new List<NativeMethods.INPUT>();
            foreach (var (vk, extended) in held) inputs.Add(Key(vk, up: true, extended));

            var modifiers = new List<int>();
            if (shortcut.Win) modifiers.Add(KeyNames.VK_LWIN);
            if (shortcut.Ctrl) modifiers.Add(KeyNames.VK_CONTROL);
            if (shortcut.Alt) modifiers.Add(KeyNames.VK_MENU);
            if (shortcut.Shift) modifiers.Add(KeyNames.VK_SHIFT);
            foreach (int vk in modifiers) inputs.Add(Key(vk, up: false, extended: vk == KeyNames.VK_LWIN));
            inputs.Add(Key(shortcut.Code, up: false));
            inputs.Add(Key(shortcut.Code, up: true));
            foreach (int vk in Enumerable.Reverse(modifiers)) inputs.Add(Key(vk, up: true, extended: vk == KeyNames.VK_LWIN));

            foreach (var (vk, extended) in held) inputs.Add(Key(vk, up: false, extended));
            if (held.Any(k => k.Vk is KeyNames.VK_LWIN or KeyNames.VK_RWIN or KeyNames.VK_LMENU or KeyNames.VK_RMENU))
            {
                // Releasing Win or Alt alone would open the Start menu or the menu bar.
                inputs.Add(Key(0xFF, up: false));
                inputs.Add(Key(0xFF, up: true));
            }

            var array = inputs.ToArray();
            string heldNames = string.Join(", ", held.Select(k => KeyNames.GetName(k.Vk)));
            uint sent = NativeMethods.SendInput((uint)array.Length, array, Marshal.SizeOf<NativeMethods.INPUT>());
            if (sent != array.Length) Logger.Warn($"SendInput sent {sent} of {array.Length} events (error {Marshal.GetLastWin32Error()})");
            else Logger.Info($"Application mute shortcut {shortcut} sent, held modifiers: {heldNames}");
        }
    }

    private static bool IsDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

    // Browsers derive KeyboardEvent.code from the scan code, so it must be set along with the virtual key.
    private static NativeMethods.INPUT Key(int vk, bool up, bool extended = false) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = (ushort)vk,
                wScan = (ushort)NativeMethods.MapVirtualKey((uint)vk, 0 /* MAPVK_VK_TO_VSC */),
                dwFlags = (up ? NativeMethods.KEYEVENTF_KEYUP : 0) | (extended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0),
                dwExtraInfo = KeyboardHook.InjectedMarker,
            },
        },
    };
}
