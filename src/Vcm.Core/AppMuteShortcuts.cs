namespace Vcm.Core;

/// <summary>The mute shortcuts of common meeting applications, matched on the foreground window.</summary>
public static class AppMuteShortcuts
{
    private static readonly string[] BrowserProcesses = ["chrome", "msedge", "firefox", "brave", "opera", "vivaldi", "arc"];

    private static readonly HotkeySettings JitsiKey = new(false, false, false, false, 'M');
    private static readonly HotkeySettings ZoomKey = new(false, false, true, false, 'A');
    private static readonly HotkeySettings TeamsKey = new(false, true, false, true, 'M');
    private static readonly HotkeySettings MeetKey = new(false, true, false, false, 'D');
    private static readonly HotkeySettings DiscordKey = new(false, true, false, true, 'M');

    /// <param name="processName">Process name without extension, e.g. <c>chrome</c>.</param>
    /// <param name="windowTitle">Title of the foreground window.</param>
    /// <returns>The shortcut that toggles the microphone in that window, or null when it is not a known meeting.</returns>
    public static HotkeySettings? Find(string processName, string windowTitle)
    {
        bool browser = BrowserProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase);
        bool Is(string name) => processName.Equals(name, StringComparison.OrdinalIgnoreCase);
        bool Title(string text) => windowTitle.Contains(text, StringComparison.OrdinalIgnoreCase);

        // Jitsi first: its title contains "Meet" too.
        if (Is("Jitsi Meet") || (browser && Title("Jitsi"))) return JitsiKey;
        if (Is("Zoom")) return ZoomKey;
        if (Is("ms-teams") || Is("Teams") || (browser && Title("Microsoft Teams"))) return TeamsKey;
        if (Is("Discord") || (browser && Title("Discord"))) return DiscordKey;
        if (browser && (windowTitle.StartsWith("Meet - ", StringComparison.OrdinalIgnoreCase) || Title("Google Meet"))) return MeetKey;
        return null;
    }
}
