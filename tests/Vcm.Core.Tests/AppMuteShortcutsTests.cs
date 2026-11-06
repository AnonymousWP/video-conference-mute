using Vcm.Core;

namespace Vcm.Core.Tests;

public class AppMuteShortcutsTests
{
    [Theory]
    [InlineData("chrome", "room | Jitsi Meet - Google Chrome", "M")]
    [InlineData("Jitsi Meet", "Jitsi Meet", "M")]
    [InlineData("Zoom", "Zoom Meeting", "Alt + A")]
    [InlineData("ms-teams", "Standup | Microsoft Teams", "Ctrl + Shift + M")]
    [InlineData("msedge", "Chat | Microsoft Teams - Edge", "Ctrl + Shift + M")]
    [InlineData("Discord", "#voice - Discord", "Ctrl + Shift + M")]
    [InlineData("firefox", "Meet - abc-defg-hij - Firefox", "Ctrl + D")]
    public void Find_MatchesKnownMeetingApps(string process, string title, string expected)
    {
        Assert.Equal(expected, AppMuteShortcuts.Find(process, title)?.ToString());
    }

    [Theory]
    [InlineData("notepad", "Jitsi notes.txt - Notepad")]
    [InlineData("chrome", "Inbox - Mail")]
    [InlineData("code", "Meet - Visual Studio Code")]
    public void Find_IgnoresOtherWindows(string process, string title)
    {
        Assert.Null(AppMuteShortcuts.Find(process, title));
    }

    [Fact]
    public void KeySequence_NeverPressesLiftedModifiersAgain()
    {
        var zoom = new HotkeySettings(false, false, true, false, 'A');

        var keys = AppMuteKeySequence.Build(zoom, [KeyNames.VK_LWIN, KeyNames.VK_LSHIFT]);

        Assert.DoesNotContain(keys, k => !k.Up && (k.Vk == KeyNames.VK_LWIN || k.Vk == KeyNames.VK_LSHIFT));
        Assert.Contains(new AppMuteKey(KeyNames.VK_LWIN, Up: true), keys);
        Assert.Contains(new AppMuteKey(KeyNames.VK_LSHIFT, Up: true), keys);
    }

    [Fact]
    public void KeySequence_EveryKeyDownIsReleased()
    {
        var teams = new HotkeySettings(false, true, false, true, 'M');

        var keys = AppMuteKeySequence.Build(teams, [KeyNames.VK_LWIN, KeyNames.VK_RSHIFT]);

        foreach (var vk in keys.Where(k => !k.Up).Select(k => k.Vk).Distinct())
        {
            Assert.Equal(keys.Count(k => k.Vk == vk && !k.Up), keys.Count(k => k.Vk == vk && k.Up));
        }
    }

    [Fact]
    public void KeySequence_UnassignedKeyOnlyWhenWinOrAltLifted()
    {
        var jitsi = new HotkeySettings(false, false, false, false, 'M');
        var withShift = AppMuteKeySequence.Build(jitsi, [KeyNames.VK_LSHIFT]);
        var withWin = AppMuteKeySequence.Build(jitsi, [KeyNames.VK_LWIN]);

        Assert.DoesNotContain(withShift, k => k.Vk == AppMuteKeySequence.UnassignedKey);
        Assert.Contains(withWin, k => k.Vk == AppMuteKeySequence.UnassignedKey);
    }
}
