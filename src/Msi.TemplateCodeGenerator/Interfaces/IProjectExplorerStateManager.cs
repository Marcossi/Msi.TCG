using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Gestiona el estado de UI del explorador de proyectos en memoria.
/// Maneja la captura y restauración del estado de expansión del árbol.
/// </summary>
internal interface IProjectExplorerStateManager
{
    /// <summary>
    /// Captura las rutas de los nodos expandidos en el árbol.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    /// <returns>Conjunto de rutas expandidas.</returns>
    HashSet<string> CaptureExpandedPaths(IEnumerable<FileEntryViewModel> fileTree);

    /// <summary>
    /// Restaura el estado de expansión en el árbol desde un conjunto de rutas guardadas.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    /// <param name="expandedPaths">Rutas que deben estar expandidas.</param>
    void RestoreExpandedState(IEnumerable<FileEntryViewModel> fileTree, HashSet<string> expandedPaths);

    /// <summary>
    /// Guarda el estado de UI del explorador.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    /// <param name="projectPath">Ruta del proyecto.</param>
    Task SaveStateAsync(IEnumerable<FileEntryViewModel> fileTree, string projectPath);

    /// <summary>
    /// Restaura el estado de UI del explorador desde el servicio de persistencia.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    /// <param name="projectPath">Ruta del proyecto.</param>
    Task RestoreUiStateAsync(IEnumerable<FileEntryViewModel> fileTree, string projectPath);
}
