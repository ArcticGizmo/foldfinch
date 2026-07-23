namespace Foldfinch.Core.Pdf;

/// <summary>
/// A PDF file that has been opened into the working set. Pages in <see cref="PdfDocumentModel"/>
/// reference their source by <see cref="Id"/>, so the same file can contribute pages even after
/// they've been reordered or interleaved with other sources.
/// </summary>
public sealed record SourceDocument
{
    /// <summary>Stable identifier used by <see cref="PageRef.SourceId"/> (unique within a model).</summary>
    public required string Id { get; init; }

    /// <summary>Absolute path the file was opened from — used again at save time to import pages.</summary>
    public required string Path { get; init; }

    /// <summary>File name (no directory) for display in the UI.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Number of pages in the source file.</summary>
    public required int PageCount { get; init; }
}
