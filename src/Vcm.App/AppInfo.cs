using System.Reflection;

namespace Vcm.App;

internal static class AppInfo
{
    /// <summary>The &lt;Version&gt; the app was built with, without the "+commit" metadata the SDK appends.</summary>
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0] ?? "?";
}
