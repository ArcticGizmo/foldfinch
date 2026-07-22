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
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private EditorViewModel? Vm => DataContext as EditorViewModel;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
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
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _isReorderDrag ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (Vm is null || !_isReorderDrag) return;

        var root = ItemRoot(e.Source);
        int target;
        if (root?.DataContext is PageThumbnailViewModel tile)
        {
            target = Vm.Pages.IndexOf(tile);
            if (e.GetPosition(root).X > root.Bounds.Width / 2) target += 1; // right half = "after"
        }
        else
        {
            target = Vm.Pages.Count; // dropped on empty canvas -> move to the end
        }

        Vm.MoveSelectionTo(target);
        e.Handled = true;
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
