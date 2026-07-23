namespace Foldfinch.Core.Pdf;

/// <summary>
/// The in-memory working document: the set of opened source files plus an ordered list of page
/// references. Every operation the user performs — remove, reorder, rotate, combine — is a pure
/// transform over this list, with no PDF I/O. <see cref="PdfEditor"/> turns it back into a file.
/// </summary>
public sealed class PdfDocumentModel
{
    private readonly List<SourceDocument> _sources = [];
    private readonly List<PageRef> _pages = [];

    /// <summary>The source files contributing pages, in the order they were added.</summary>
    public IReadOnlyList<SourceDocument> Sources => _sources;

    /// <summary>The ordered pages of the working document.</summary>
    public IReadOnlyList<PageRef> Pages => _pages;

    /// <summary>Number of pages currently in the working document.</summary>
    public int PageCount => _pages.Count;

    /// <summary>Look up a source by id (null if not present).</summary>
    public SourceDocument? FindSource(string id) => _sources.FirstOrDefault(s => s.Id == id);

    /// <summary>
    /// Registers a source and, by default, appends all of its pages (in order) to the document.
    /// This is how both "open" (first source) and "combine" (subsequent sources) work.
    /// </summary>
    public void AddSource(SourceDocument source, bool appendAllPages = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        Register(source);
        if (appendAllPages)
            for (var i = 0; i < source.PageCount; i++)
                _pages.Add(new PageRef(source.Id, i));
    }

    /// <summary>
    /// Registers a source and inserts all of its pages starting at <paramref name="atPageIndex"/>
    /// (clamped to the current range). This backs "insert a PDF at this spot".
    /// </summary>
    public void AddSourceAt(SourceDocument source, int atPageIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        Register(source);
        var at = Math.Clamp(atPageIndex, 0, _pages.Count);
        var pages = Enumerable.Range(0, source.PageCount).Select(i => new PageRef(source.Id, i));
        _pages.InsertRange(at, pages);
    }

    private void Register(SourceDocument source)
    {
        if (_sources.Any(s => s.Id == source.Id))
            throw new ArgumentException($"A source with id '{source.Id}' is already present.", nameof(source));
        _sources.Add(source);
    }

    /// <summary>Removes the pages at the given indices (order-independent; duplicates ignored).</summary>
    public void RemoveAt(IEnumerable<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        // Remove from the back so earlier indices stay valid.
        foreach (var i in indices.Distinct().OrderByDescending(i => i))
        {
            ValidateIndex(i);
            _pages.RemoveAt(i);
        }
    }

    /// <summary>Moves the page at <paramref name="fromIndex"/> so it lands at <paramref name="toIndex"/>.</summary>
    public void Move(int fromIndex, int toIndex)
    {
        ValidateIndex(fromIndex);
        if (toIndex < 0 || toIndex >= _pages.Count)
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        if (fromIndex == toIndex) return;

        var page = _pages[fromIndex];
        _pages.RemoveAt(fromIndex);
        _pages.Insert(toIndex, page);
    }

    /// <summary>
    /// Replaces the whole page order with <paramref name="newOrder"/>. The new list must be a
    /// permutation of the current pages (same instances, no additions or removals) — this is what a
    /// drag-and-drop reorder produces.
    /// </summary>
    public void Reorder(IReadOnlyList<PageRef> newOrder)
    {
        ArgumentNullException.ThrowIfNull(newOrder);
        if (newOrder.Count != _pages.Count)
            throw new ArgumentException("Reorder must be a permutation of the current pages.", nameof(newOrder));

        // Cheap multiset check: same references, same multiplicity.
        var current = new HashSet<PageRef>(_pages, ReferenceEqualityComparer.Instance);
        if (!newOrder.All(p => current.Contains(p)) || newOrder.Distinct().Count() != newOrder.Count)
            throw new ArgumentException("Reorder must be a permutation of the current pages.", nameof(newOrder));

        _pages.Clear();
        _pages.AddRange(newOrder);
    }

    /// <summary>Rotates the page at <paramref name="index"/> by <paramref name="deltaDegrees"/> (clockwise).</summary>
    public void Rotate(int index, int deltaDegrees)
    {
        ValidateIndex(index);
        _pages[index] = _pages[index].Rotated(deltaDegrees);
    }

    /// <summary>Captures the current page list (for undo/redo). Cheap — <see cref="PageRef"/> is immutable.</summary>
    public IReadOnlyList<PageRef> Snapshot() => [.. _pages];

    /// <summary>
    /// Replaces the whole page list with <paramref name="pages"/>. Every entry must reference a known
    /// source and a valid page index within it. Used to apply an undo/redo snapshot.
    /// </summary>
    public void Restore(IReadOnlyList<PageRef> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        foreach (var p in pages)
        {
            var source = FindSource(p.SourceId)
                ?? throw new ArgumentException($"Snapshot references unknown source '{p.SourceId}'.", nameof(pages));
            if (p.SourcePageIndex < 0 || p.SourcePageIndex >= source.PageCount)
                throw new ArgumentException($"Snapshot page index {p.SourcePageIndex} is out of range for '{source.DisplayName}'.", nameof(pages));
        }

        _pages.Clear();
        _pages.AddRange(pages);
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= _pages.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Page index {index} is out of range (0..{_pages.Count - 1}).");
    }
}
