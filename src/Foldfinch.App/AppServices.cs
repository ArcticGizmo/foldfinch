namespace Foldfinch.App;

/// <summary>
/// Composition root: holds the services the UI drives. PDF work in <c>Foldfinch.Core</c> is
/// synchronous/blocking, so ViewModels run it off the UI thread via <see cref="RunAsync"/>.
/// </summary>
public sealed class AppServices
{
    // Core services (renderer, editor) are wired in from M1/M3 onward.

    /// <summary>Run a blocking Core call on a background thread (keeps the UI responsive).</summary>
    public static Task<T> RunAsync<T>(Func<T> work) => Task.Run(work);

    public static Task RunAsync(Action work) => Task.Run(work);
}
