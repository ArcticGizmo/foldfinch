using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Foldfinch.App.Rendering;
using Foldfinch.Core.Pdf;

namespace Foldfinch.App.ViewModels;

/// <summary>One page tile in the editor grid: the page reference, its source, and a lazily rendered thumbnail.</summary>
public partial class PageThumbnailViewModel : ViewModelBase
{
    /// <summary>Target thumbnail width in pixels (aspect preserved by the renderer).</summary>
    public const int ThumbnailWidth = 200;

    /// <summary>The page this tile represents (identity + rotation).</summary>
    public PageRef Page { get; }

    /// <summary>Absolute path of the source PDF this page comes from.</summary>
    public string SourcePath { get; }

    /// <summary>1-based position of this page in the working document.</summary>
    [ObservableProperty] private int _pageNumber;

    /// <summary>Source-file name shown on the tile when the document combines more than one file.</summary>
    public string SourceLabel { get; init; } = "";

    /// <summary>Per-source colour swatch (shown alongside the label); null when there's a single source.</summary>
    public IBrush? SourceColor { get; init; }

    /// <summary>Whether to show the source chip (true only when the document has multiple sources).</summary>
    public bool ShowSource { get; init; }

    /// <summary>True for the last page — enables the trailing "insert at end" affordance.</summary>
    public bool IsLast { get; init; }

    /// <summary>The rendered thumbnail (null until loaded).</summary>
    [ObservableProperty] private Bitmap? _image;

    /// <summary>True while the thumbnail is being rendered.</summary>
    [ObservableProperty] private bool _isLoading = true;

    /// <summary>True when this tile is part of the current selection (drives the highlight).</summary>
    [ObservableProperty] private bool _isSelected;

    public PageThumbnailViewModel(PageRef page, string sourcePath, int pageNumber)
    {
        Page = page;
        SourcePath = sourcePath;
        _pageNumber = pageNumber;
    }

    /// <summary>Renders the thumbnail (respecting the page's rotation) via the shared service.</summary>
    public async Task LoadAsync(ThumbnailService thumbnails)
    {
        IsLoading = true;
        try
        {
            Image = await thumbnails.GetAsync(SourcePath, Page.SourcePageIndex, Page.Rotation, ThumbnailWidth);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
