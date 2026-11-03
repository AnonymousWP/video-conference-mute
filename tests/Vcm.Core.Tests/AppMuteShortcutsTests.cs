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
}
