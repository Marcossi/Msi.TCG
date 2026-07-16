using System.Collections.ObjectModel;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Construye la jerarquía de ficheros del explorador de proyectos
/// a partir de la lista plana de FileEntry del modelo de dominio.
/// </summary>
internal interface IProjectTreeBuilder
{
    /// <summary>
    /// Construye el árbol de <see cref="FileEntryViewModel"/> a partir de la lista plana del modelo de dominio.
    /// </summary>
    /// <param name="project">Proyecto con la lista de ficheros.</param>
    /// <param name="projectFilePath">Ruta al fichero .scribanproj.</param>
    /// <param name="expandedPaths">Paths de carpetas expandidas (para restaurar estado de UI).</param>
    /// <returns>Árbol jerárquico de entradas.</returns>
    ObservableCollection<FileEntryViewModel> BuildFileTree(
        Models.Project project,
        string projectFilePath,
        IReadOnlySet<string> expandedPaths);
}
