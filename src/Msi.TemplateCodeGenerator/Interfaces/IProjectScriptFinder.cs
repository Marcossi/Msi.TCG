using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio para buscar scripts en el árbol de archivos del proyecto.
/// </summary>
internal interface IProjectScriptFinder
{
    /// <summary>
    /// Busca todos los scripts en el árbol de archivos.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    /// <returns>Colección de entradas de tipo Script.</returns>
    IEnumerable<FileEntryViewModel> FindAllScripts(IEnumerable<FileEntryViewModel> fileTree);
}
