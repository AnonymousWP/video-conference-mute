namespace Vcm.Core;

public readonly record struct AppMuteKey(int Vk, bool Up);

/// <summary>The key events that make a meeting application toggle its own mute.</summary>
public static class AppMuteKeySequence
{
    /// <summary>Key that no application binds; pressing it stops Windows from reading a lone Win or Alt release as a menu action.</summary>
    public const int UnassignedKey = 0xFF;

    /// <param name="shortcut">The application's mute shortcut.</param>
    /// <param name="heldModifiers">Modifiers still physically held from VCM's own shortcut; they are lifted so they do not combine with the key.</param>
    /// <remarks>
    /// Lifted modifiers are deliberately not pressed again: if the user releases them in the meantime the press would
    /// never be undone, and the stuck modifier turns later typing into shortcuts.
    /// </remarks>
    public static IReadOnlyList<AppMuteKey> Build(HotkeySettings shortcut, IEnumerable<int> heldModifiers)
    {
        var held = heldModifiers.ToList();
        var keys = new List<AppMuteKey>();

        if (held.Any(vk => vk is KeyNames.VK_LWIN or KeyNames.VK_RWIN or KeyNames.VK_LMENU or KeyNames.VK_RMENU))
        {
            keys.Add(new AppMuteKey(UnassignedKey, Up: false));
            keys.Add(new AppMuteKey(UnassignedKey, Up: true));
        }

        foreach (int vk in held) keys.Add(new AppMuteKey(vk, Up: true));

        var modifiers = new List<int>();
        if (shortcut.Win) modifiers.Add(KeyNames.VK_LWIN);
        if (shortcut.Ctrl) modifiers.Add(KeyNames.VK_CONTROL);
        if (shortcut.Alt) modifiers.Add(KeyNames.VK_MENU);
        if (shortcut.Shift) modifiers.Add(KeyNames.VK_SHIFT);

        foreach (int vk in modifiers) keys.Add(new AppMuteKey(vk, Up: false));
        keys.Add(new AppMuteKey(shortcut.Code, Up: false));
        keys.Add(new AppMuteKey(shortcut.Code, Up: true));
        foreach (int vk in Enumerable.Reverse(modifiers)) keys.Add(new AppMuteKey(vk, Up: true));

        return keys;
    }
}
