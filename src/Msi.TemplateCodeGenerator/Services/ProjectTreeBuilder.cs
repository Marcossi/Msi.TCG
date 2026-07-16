using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IProjectTreeBuilder"/> que construye la jerarquía
/// de ficheros del explorador de proyectos a partir de la lista plana de FileEntry.
/// </summary>
internal sealed class ProjectTreeBuilder(
    ILogger<ProjectTreeBuilder> logger,
    ILogger<FileEntryViewModel> fileEntryLogger) : IProjectTreeBuilder
{
    private readonly ILogger<ProjectTreeBuilder> _logger = logger;
    private readonly ILogger<FileEntryViewModel> _fileEntryLogger = fileEntryLogger;

    /// <inheritdoc/>
    public ObservableCollection<FileEntryViewModel> BuildFileTree(
        Models.Project project,
        string projectFilePath,
        IReadOnlySet<string> expandedPaths)
    {
        string projectFileName = Path.GetFileName(projectFilePath);
        bool isFirstLoad = expandedPaths.Count == 0;

        Dictionary<string, FileEntryViewModel> dict = new(StringComparer.OrdinalIgnoreCase);
        List<FileEntryViewModel> roots = new();

        // Ordenar globalmente: directorios (0) antes que ficheros (1), después alfabético por ruta.
        // Esto garantiza a la vez que los padres se procesan antes que sus hijos
        // y que dentro de cada nodo los directorios aparezcan antes que los ficheros.
        foreach (FileEntry entry in project.Files
            .Where(f => !f.RelativePath.Replace('\\', '/').Equals(projectFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Type == FileType.Directory ? 0 : 1)
            .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            string normalizedPath = entry.RelativePath.Replace('\\', '/');
            FileEntryViewModel vm = new(entry.Name, normalizedPath, entry.Type, _fileEntryLogger)
            {
                IsExpanded = expandedPaths.Contains(normalizedPath)
            };
            dict[normalizedPath] = vm;

            int lastSlash = normalizedPath.LastIndexOf('/');
            string? parentPath = lastSlash < 0 ? null : normalizedPath[..lastSlash];

            if (parentPath is not null && dict.TryGetValue(parentPath, out FileEntryViewModel? parent))
                parent.Children.Add(vm);
            else
                roots.Add(vm);
        }

        // Nodo raíz: representa el propio fichero .scribanproj
        // En la primera carga se expande por defecto; en refreshes se respeta el estado del usuario.
        FileEntryViewModel projectRoot = new(projectFileName, string.Empty, FileType.Project, _fileEntryLogger)
        {
            IsExpanded = isFirstLoad || expandedPaths.Contains(string.Empty)
        };
        foreach (FileEntryViewModel root in roots)
            projectRoot.Children.Add(root);

        _logger.LogDebug("Árbol construido: {RootCount} nodos raíz, {TotalCount} entradas totales",
            roots.Count, dict.Count);

        return new ObservableCollection<FileEntryViewModel> { projectRoot };
    }
}
