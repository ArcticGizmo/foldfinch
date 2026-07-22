using Foldfinch.App.Services;

namespace Foldfinch.Tests.App;

/// <summary>Test double for the file pickers: open returns queued paths, save returns a preset path.</summary>
internal sealed class FakeFileDialogService : IFileDialogService
{
    public Queue<string?> OpenResults { get; } = new();
    public string? SaveResult { get; set; }

    public Task<string?> OpenPdfAsync(string title) =>
        Task.FromResult(OpenResults.Count > 0 ? OpenResults.Dequeue() : null);

    public Task<string?> SavePdfAsync(string suggestedName) => Task.FromResult(SaveResult);
}
