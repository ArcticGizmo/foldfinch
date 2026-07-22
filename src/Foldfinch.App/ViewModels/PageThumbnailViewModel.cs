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
