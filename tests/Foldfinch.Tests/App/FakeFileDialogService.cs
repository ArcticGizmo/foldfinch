using Foldfinch.App.Services;

namespace Foldfinch.Tests.App;

/// <summary>Test double for the file pickers: open returns queued paths, save returns a preset path.</summary>
internal sealed class FakeFileDialogService : IFileDialogService
{
    public Queue<string?> OpenResults { get; } = new();
    public string? SaveResult { get; set; }

    public Task<IReadOnlyList<string>> OpenPdfsAsync(string title)
    {
        var next = OpenResults.Count > 0 ? OpenResults.Dequeue() : null;
        return Task.FromResult<IReadOnlyList<string>>(next is null ? [] : [next]);
    }

    public Task<string?> SavePdfAsync(string suggestedName) => Task.FromResult(SaveResult);
}
