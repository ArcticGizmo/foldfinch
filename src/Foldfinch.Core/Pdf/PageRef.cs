namespace Foldfinch.Core.Pdf;

/// <summary>
/// A single page in the working document: a reference to a page in some <see cref="SourceDocument"/>
/// plus the extra rotation the user has applied. Immutable — rotating produces a new instance, which
/// keeps operations easy to reason about (and to undo).
/// </summary>
/// <param name="SourceId">The <see cref="SourceDocument.Id"/> this page comes from.</param>
/// <param name="SourcePageIndex">Zero-based page index within the source document.</param>
/// <param name="Rotation">Extra clockwise rotation in degrees, always normalised to 0/90/180/270.</param>
public sealed record PageRef(string SourceId, int SourcePageIndex, int Rotation = 0)
{
    /// <summary>Returns this page with <paramref name="deltaDegrees"/> added, normalised to 0/90/180/270.</summary>
    public PageRef Rotated(int deltaDegrees) => this with { Rotation = Rotations.Normalize(Rotation + deltaDegrees) };
}

/// <summary>Helpers for keeping rotation values within the canonical 0/90/180/270 range.</summary>
public static class Rotations
{
    /// <summary>Normalises any degree value to one of 0, 90, 180, 270 (clockwise, wrapping negatives).</summary>
    public static int Normalize(int degrees)
    {
        var r = degrees % 360;
        if (r < 0) r += 360;
        // Snap to the nearest right angle so callers can't smuggle in odd values.
        return (r / 90 * 90) % 360;
    }
}
