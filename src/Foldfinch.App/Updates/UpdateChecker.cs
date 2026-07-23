using System;
using System.Threading.Tasks;
using Velopack;

namespace Foldfinch.App.Updates;

/// <summary>Outcome of an update check.</summary>
public enum UpdateAvailability { NotApplicable, UpToDate, Available, Failed }

/// <summary>Result of an update check, carrying the Velopack handles needed to apply it.</summary>
public sealed class UpdateCheckResult
{
    public required UpdateAvailability Availability { get; init; }
    public string? CurrentVersion { get; init; }
    public string? AvailableVersion { get; init; }

    internal UpdateManager? Manager { get; init; }
    internal UpdateInfo? Info { get; init; }
}

/// <summary>
/// Checks a release feed for a newer version and (from About) applies it. The launch-time banner is
/// notification-only. The feed comes from <c>FOLDFINCH_UPDATE_FEED</c> (a directory or URL); if unset,
/// or the app wasn't installed via Velopack, the check is a no-op. Failures are swallowed.
/// </summary>
public static class UpdateChecker
{
    public const string FeedEnvVar = "FOLDFINCH_UPDATE_FEED";

    public static async Task<UpdateCheckResult> CheckDetailedAsync()
    {
        var feed = Environment.GetEnvironmentVariable(FeedEnvVar);
        if (string.IsNullOrWhiteSpace(feed))
            return new UpdateCheckResult { Availability = UpdateAvailability.NotApplicable };

        try
        {
            var manager = new UpdateManager(feed);
            if (!manager.IsInstalled)
                return new UpdateCheckResult { Availability = UpdateAvailability.NotApplicable };

            var current = manager.CurrentVersion?.ToString();
            var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return new UpdateCheckResult { Availability = UpdateAvailability.UpToDate, CurrentVersion = current };

            return new UpdateCheckResult
            {
                Availability = UpdateAvailability.Available,
                CurrentVersion = current,
                AvailableVersion = update.TargetFullRelease.Version.ToString(),
                Manager = manager,
                Info = update,
            };
        }
        catch
        {
            return new UpdateCheckResult { Availability = UpdateAvailability.Failed };
        }
    }

    /// <summary>Downloads, installs, and restarts into the update. Does not return on success.</summary>
    public static async Task ApplyAsync(UpdateCheckResult result)
    {
        if (result is not { Availability: UpdateAvailability.Available, Manager: { } manager, Info: { } info })
            return;

        await manager.DownloadUpdatesAsync(info).ConfigureAwait(false);
        manager.ApplyUpdatesAndRestart(info.TargetFullRelease);
    }

    /// <summary>A human-readable notice when a newer release exists, else null.</summary>
    public static async Task<string?> CheckAsync()
    {
        var result = await CheckDetailedAsync().ConfigureAwait(false);
        return result.Availability == UpdateAvailability.Available
            ? $"Update available: v{result.AvailableVersion} — you have v{result.CurrentVersion}"
            : null;
    }
}
