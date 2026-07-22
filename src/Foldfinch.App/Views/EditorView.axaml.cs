using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Foldfinch.App.ViewModels;

namespace Foldfinch.App.Views;

/// <summary>
/// The page grid. Handles pointer selection (plain / Ctrl / Shift) and drag-and-drop reordering
/// (Avalonia 12 DataTransfer API); the actual list mutations live in <see cref="EditorViewModel"/>.
/// </summary>
public partial class EditorView : UserControl
{
    private const double DragThreshold = 5;

    private Point _pressPoint;
    private PageThumbnailViewModel? _pressTile;
    private PointerPressedEventArgs? _pressArgs;
    private bool _dragging;
    private bool _isReorderDrag; // true while our own reorder drag is in flight

    public EditorView()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, (_, _) => HideDropLine());
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private EditorViewModel? Vm => DataContext as EditorViewModel;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Clicks on the "+" insert affordance run their own command; don't select/drag the page.
        if (IsInsertAffordance(e.Source)) { _pressTile = null; return; }

        _dragging = false;
        _pressTile = TileFrom(e.Source);
        _pressArgs = e;
        _pressPoint = e.GetPosition(this);
        if (_pressTile is null || Vm is null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var mods = e.KeyModifiers;
        if (mods.HasFlag(KeyModifiers.Shift))
            Vm.SelectRange(_pressTile);
        else if (mods.HasFlag(KeyModifiers.Control) || mods.HasFlag(KeyModifiers.Meta))
            Vm.ToggleSelect(_pressTile);
        else if (!_pressTile.IsSelected)
            Vm.SelectSingle(_pressTile); // an already-selected tile is left alone so a multi-drag can start
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pressTile is null || _dragging || _pressArgs is null || Vm is null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var delta = e.GetPosition(this) - _pressPoint;
        if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold) return;

        if (!_pressTile.IsSelected) Vm.SelectSingle(_pressTile);
        _dragging = true;
        _ = StartReorderDragAsync(_pressArgs);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        // A plain click (no drag, no modifier) on a tile within a multi-selection collapses to just it.
        if (!_dragging && _pressTile is not null && Vm is not null
            && e.KeyModifiers == KeyModifiers.None
            && _pressTile.IsSelected && Vm.SelectionCount > 1)
        {
            Vm.SelectSingle(_pressTile);
        }

        _pressTile = null;
        _pressArgs = null;
        _dragging = false;
    }

    private async Task StartReorderDragAsync(PointerPressedEventArgs trigger)
    {
        var item = new DataTransferItem();
        item.Set(DataFormat.Text, "foldfinch-page");
        var data = new DataTransfer();
        data.Add(item);

        _isReorderDrag = true;
        try
        {
            await DragDrop.DoDragDropAsync(trigger, data, DragDropEffects.Move);
        }
        finally
        {
            _isReorderDrag = false;
            HideDropLine();
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _isReorderDrag ? DragDropEffects.Move : DragDropEffects.None;
        if (_isReorderDrag) ShowDropLine(e);
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        HideDropLine();
        if (Vm is null || !_isReorderDrag) return;

        Vm.MoveSelectionTo(DropTarget(e));
        e.Handled = true;
    }

    /// <summary>The insertion index the current pointer position maps to.</summary>
    private int DropTarget(DragEventArgs e)
    {
        if (Vm is null) return 0;
        var root = ItemRoot(e.Source);
        if (root?.DataContext is not PageThumbnailViewModel tile) return Vm.Pages.Count; // empty area -> end

        var target = Vm.Pages.IndexOf(tile);
        if (e.GetPosition(root).X > root.Bounds.Width / 2) target += 1; // right half = "after"
        return target;
    }

    /// <summary>Draws the insertion line at the left/right edge of the tile under the pointer.</summary>
    private void ShowDropLine(DragEventArgs e)
    {
        var root = ItemRoot(e.Source);
        if (root?.DataContext is not PageThumbnailViewModel)
        {
            HideDropLine();
            return;
        }

        var after = e.GetPosition(root).X > root.Bounds.Width / 2;
        var edgeX = after ? root.Bounds.Width : 0;
        var origin = root.TranslatePoint(new Point(edgeX, 0), DropOverlay);
        if (origin is not { } p)
        {
            HideDropLine();
            return;
        }

        Canvas.SetLeft(DropLine, p.X - DropLine.Width / 2);
        Canvas.SetTop(DropLine, p.Y + 6);
        DropLine.Height = System.Math.Max(0, root.Bounds.Height - 12);
        DropLine.IsVisible = true;
    }

    private void HideDropLine() => DropLine.IsVisible = false;

    /// <summary>True when <paramref name="source"/> is inside the "+" insert button.</summary>
    private static bool IsInsertAffordance(object? source)
    {
        for (var v = source as Visual; v is not null; v = v.GetVisualParent())
            if (v is Button b && b.Classes.Contains("insert"))
                return true;
        return false;
    }

    /// <summary>The tile view-model whose visual subtree contains <paramref name="source"/> (or null).</summary>
    private static PageThumbnailViewModel? TileFrom(object? source) =>
        (source as StyledElement)?.DataContext as PageThumbnailViewModel;

    /// <summary>The outermost visual whose DataContext is a page tile — used for hit bounds on drop.</summary>
    private static Control? ItemRoot(object? source)
    {
        Control? root = null;
        for (var v = source as Visual; v is not null; v = v.GetVisualParent())
            if (v is Control c && c.DataContext is PageThumbnailViewModel)
                root = c;
        return root;
    }
}
