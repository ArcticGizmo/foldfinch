using System.Reflection;

namespace Foldfinch.App;

/// <summary>The running app version, resolved once from the assembly's informational version.</summary>
public static class AppVersion
{
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
            return info.Split('+')[0]; // strip any "+<sha>" build metadata

        var v = asm.GetName().Version;
        return v is null ? "unknown" : $"{v.Major}.{v.Minor}.{v.Build}";
    }
}
