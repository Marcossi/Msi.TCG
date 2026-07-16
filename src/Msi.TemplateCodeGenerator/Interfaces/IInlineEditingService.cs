using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio para manejar la edición inline de nombres en el explorador de proyectos.
/// </summary>
internal interface IInlineEditingService
{
    /// <summary>
    /// Inicia el modo de edición inline para renombrar la entrada indicada.
    /// Cancela cualquier edición activa en otros nodos antes de activar la nueva.
    /// </summary>
    /// <param name="entry">Entrada a renombrar.</param>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    void StartRename(FileEntryViewModel? entry, IEnumerable<FileEntryViewModel> fileTree);

    /// <summary>
    /// Confirma la edición inline: renombra el elemento si el nombre ha cambiado.
    /// </summary>
    /// <param name="entry">Entrada en edición.</param>
    /// <param name="projectService">Servicio de proyecto para ejecutar el renombrado.</param>
    /// <returns>True si el renombrado fue exitoso, false en caso contrario.</returns>
    Task<bool> ConfirmRenameAsync(FileEntryViewModel? entry, IProjectService projectService);

    /// <summary>
    /// Cancela la edición inline.
    /// </summary>
    /// <param name="entry">Entrada en edición.</param>
    void CancelRename(FileEntryViewModel? entry);
}
