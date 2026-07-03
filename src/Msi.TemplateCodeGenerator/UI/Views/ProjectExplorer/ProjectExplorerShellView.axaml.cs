using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer;

/// <summary>
/// Lógica de interacción para ProjectExplorerShellView.axaml
/// </summary>
internal partial class ProjectExplorerShellView : UserControl
{
    public ProjectExplorerShellView()
    {
        InitializeComponent();
    }

    private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual sourceVisual)
        {
            TreeViewItem? treeViewItem = null;
            Visual? current = sourceVisual;

            while (current != null)
            {
                if (current is TreeViewItem item)
                {
                    treeViewItem = item;
                    break;
                }
                current = current.GetVisualParent();
            }

            if (treeViewItem?.DataContext is FileEntryViewModel entry &&
                DataContext is ProjectExplorerShellViewModel vm)
            {
                vm.OpenFileCommand.Execute(entry);
                e.Handled = true;
            }
        }
    }
}
