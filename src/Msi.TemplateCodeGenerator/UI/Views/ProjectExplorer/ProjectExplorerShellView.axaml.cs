using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer;

/// <summary>
/// Lógica de interacción para ProjectExplorerShellView.axaml
/// </summary>
internal partial class ProjectExplorerShellView : UserControl
{
    private bool _editHandled;
    private Point _dragStartPoint;
    private bool _isDragPending;
    private PointerPressedEventArgs? _dragTriggerEvent;
    private FileEntryViewModel? _draggedEntry;

    public ProjectExplorerShellView()
    {
        InitializeComponent();

        // Suscribirse al evento PointerPressed usando tunneling,
        // porque los TreeViewItem marcan el evento como handled internamente.
        FileTree.AddHandler(
            InputElement.PointerPressedEvent,
            OnTreeViewPointerPressed,
            RoutingStrategies.Tunnel);
    }

    // ──────────────────────────────────────────────
    //  Double-click: abrir fichero en editor
    // ──────────────────────────────────────────────

    private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual sourceVisual)
        {
            TreeViewItem? treeViewItem = FindAncestorTreeViewItem(sourceVisual);

            if (treeViewItem?.DataContext is FileEntryViewModel entry &&
                DataContext is ProjectExplorerShellViewModel vm)
            {
                vm.OpenFileCommand.Execute(entry);
                e.Handled = true;
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Inline editing: key handling
    // ──────────────────────────────────────────────

    private void OnEditingTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (_editHandled) return;

        if (DataContext is not ProjectExplorerShellViewModel vm) return;

        if (sender is not TextBox textBox) return;

        FileEntryViewModel? entry = textBox.DataContext as FileEntryViewModel;
        if (entry == null) return;

        switch (e.Key)
        {
            case Key.Enter:
                _editHandled = true;
                vm.ConfirmRenameCommand.Execute(entry);
                e.Handled = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _editHandled = false);
                break;

            case Key.Escape:
                _editHandled = true;
                entry.IsEditing = false;
                vm.CancelRenameCommand.Execute(entry);
                e.Handled = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _editHandled = false);
                break;
        }
    }

    private void OnEditingTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_editHandled) return;

        if (sender is TextBox textBox && textBox.DataContext is FileEntryViewModel entry)
        {
            if (DataContext is ProjectExplorerShellViewModel vm && entry.IsEditing)
            {
                vm.ConfirmRenameCommand.Execute(entry);
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Context menu (right-click)
    // ──────────────────────────────────────────────

    private void OnTreeViewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragPending = false;
        _dragTriggerEvent = null;
        _draggedEntry = null;

        if (e.InitialPressMouseButton != MouseButton.Right) return;

        if (e.Source is not Visual sourceVisual) return;

        TreeViewItem? item = FindAncestorTreeViewItem(sourceVisual);
        if (item?.DataContext is not FileEntryViewModel entry) return;

        if (DataContext is not ProjectExplorerShellViewModel vm) return;

        e.Handled = true;

        FileTree.SelectedItem = entry;

        ShowContextMenu(entry, vm, item);
    }

    private void ShowContextMenu(FileEntryViewModel entry, ProjectExplorerShellViewModel vm, TreeViewItem placementTarget)
    {
        IContextMenuService? contextMenuService = App.Services?.GetService(typeof(IContextMenuService)) as IContextMenuService;
        if (contextMenuService == null) return;

        IReadOnlyList<ContextMenuItem> items = contextMenuService.GetContextMenuItems(entry, vm);

        ContextMenu contextMenu = new();
        foreach (ContextMenuItem item in items)
        {
            if (item.IsSeparator)
            {
                contextMenu.Items.Add(new Separator());
            }
            else
            {
                MenuItem menuItem = new() { Header = item.Header };
                if (item.Command != null)
                {
                    menuItem.Click += (_, _) => item.Command();
                }
                contextMenu.Items.Add(menuItem);
            }
        }

        contextMenu.PlacementTarget = placementTarget;
        contextMenu.Open(placementTarget);
    }

    // ──────────────────────────────────────────────
    //  Drag & Drop
    // ──────────────────────────────────────────────

    private void OnTreeViewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed) return;
        if (e.Source is not Visual sourceVisual) return;

        TreeViewItem? item = FindAncestorTreeViewItem(sourceVisual);
        if (item?.DataContext is not FileEntryViewModel entry) return;
        if (entry.Type == FileType.Project) return;

        _dragStartPoint = e.GetPosition(sender as Visual);
        _isDragPending = true;
        _dragTriggerEvent = e;
        _draggedEntry = entry;
    }

    private async void OnTreeViewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragPending || _draggedEntry is null || _dragTriggerEvent is null) return;

        Point currentPos = e.GetPosition(sender as Visual);
        Vector delta = currentPos - _dragStartPoint;

        if (delta.Length > 5)
        {
            _isDragPending = false;

            DataTransfer dragData = new();
            dragData.Add(DataTransferItem.CreateText(_draggedEntry.RelativePath));

            await DragDrop.DoDragDropAsync(_dragTriggerEvent, dragData, DragDropEffects.Move);
            _dragTriggerEvent = null;
            _draggedEntry = null;
        }
    }

    private void OnTreeViewDragOver(object? sender, DragEventArgs e)
    {
        string? sourcePath = e.DataTransfer.TryGetText();

        if (string.IsNullOrEmpty(sourcePath))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        TreeViewItem? targetItem = FindAncestorTreeViewItem(e.Source as Visual ?? this);
        FileEntryViewModel? targetEntry = targetItem?.DataContext as FileEntryViewModel;

        IProjectFileOperations? fileOperations = App.Services?.GetService(typeof(IProjectFileOperations)) as IProjectFileOperations;
        if (fileOperations == null || !fileOperations.IsValidDropTarget(sourcePath, targetEntry))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnTreeViewDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ProjectExplorerShellViewModel vm) return;

        string? sourcePath = e.DataTransfer.TryGetText();
        if (string.IsNullOrEmpty(sourcePath)) return;

        TreeViewItem? targetItem = FindAncestorTreeViewItem(e.Source as Visual ?? this);
        FileEntryViewModel? targetEntry = targetItem?.DataContext as FileEntryViewModel;

        IProjectFileOperations? fileOperations = App.Services?.GetService(typeof(IProjectFileOperations)) as IProjectFileOperations;
        if (fileOperations == null || !fileOperations.IsValidDropTarget(sourcePath, targetEntry)) return;

        _ = vm.MoveCommand.ExecuteAsync((sourcePath, targetEntry));
        e.Handled = true;
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static TreeViewItem? FindAncestorTreeViewItem(Visual? start)
    {
        Visual? current = start;
        while (current != null)
        {
            if (current is TreeViewItem item) return item;
            current = current.GetVisualParent();
        }
        return null;
    }
}
