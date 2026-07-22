using System.Text.Json;

namespace Foldfinch.Core.Settings;

/// <summary>User/app settings persisted between runs.</summary>
public sealed class AppSettings
{
    /// <summary>The app version that last ran here — used to show "what's new" once per update.</summary>
    public string? LastSeenVersion { get; set; }

    /// <summary>Whether to pop the changelog after an update (the user can turn this off).</summary>
    public bool ShowChangelogOnUpdate { get; set; } = true;
}

/// <summary>Reads/writes <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    AppSettings Get();
    void Save(AppSettings settings);
}

/// <summary>Stores settings as JSON under the per-user application-data folder. All failures are swallowed.</summary>
public sealed class FileSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string _path;

    public FileSettingsStore(string? root = null)
    {
        var dir = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Foldfinch");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    public AppSettings Get()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch { /* fall through to defaults */ }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(settings, Json)); }
        catch { /* best effort */ }
    }
}
