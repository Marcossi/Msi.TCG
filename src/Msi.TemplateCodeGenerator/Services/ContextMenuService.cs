using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IContextMenuService"/> que genera menús contextuales
/// según el tipo de entrada del explorador de proyectos.
/// </summary>
internal sealed class ContextMenuService : IContextMenuService
{
    /// <inheritdoc/>
    public IReadOnlyList<ContextMenuItem> GetContextMenuItems(FileEntryViewModel entry, IProjectExplorerCommands viewModel)
    {
        List<ContextMenuItem> items = new();

        if (entry.Type == FileType.Directory || entry.Type == FileType.Project)
        {
            items.Add(ContextMenuItem.Item("Nuevo fichero", () => viewModel.ExecuteCreateFile(entry)));
            items.Add(ContextMenuItem.Item("Nueva carpeta", () => viewModel.ExecuteCreateDirectory(entry)));

            if (entry.Type == FileType.Directory)
            {
                items.Add(ContextMenuItem.Separator());
                items.Add(ContextMenuItem.Item("Renombrar", () => viewModel.ExecuteRename(entry)));
                items.Add(ContextMenuItem.Item("Eliminar", () => viewModel.ExecuteDelete(entry)));
            }
        }
        else
        {
            items.Add(ContextMenuItem.Item("Renombrar", () => viewModel.ExecuteRename(entry)));
            items.Add(ContextMenuItem.Item("Eliminar", () => viewModel.ExecuteDelete(entry)));
            items.Add(ContextMenuItem.Item("Duplicar", () => viewModel.ExecuteDuplicate(entry)));
        }

        items.Add(ContextMenuItem.Separator());
        items.Add(ContextMenuItem.Item("Refrescar", () => viewModel.ExecuteRefresh()));

        return items;
    }
}
