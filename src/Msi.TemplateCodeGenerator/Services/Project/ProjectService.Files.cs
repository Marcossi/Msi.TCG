using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Parte del servicio que gestiona los ficheros del proyecto:
/// refresco, clasificación y futuras operaciones de añadir/eliminar entradas.
/// </summary>
internal sealed partial class ProjectService
{
    /// <summary>
    /// Refresca la lista de ficheros del proyecto activo escaneando la carpeta raíz en disco.
    /// </summary>
    public async Task RefreshFilesAsync()
    {
        if (!_context.IsProjectOpen)
            throw new InvalidOperationException("No project is currently open.");

        Models.Project project = _context.CurrentProject!;

        if (string.IsNullOrWhiteSpace(project.FolderPath) || !await _fileSystem.DirectoryExistsAsync(project.FolderPath))
            throw new InvalidOperationException("La carpeta del proyecto no existe o su ruta no está configurada.");

        IReadOnlyList<FileSystemEntryInfo> entries = await _fileSystem.GetFileSystemInfosAsync(
            project.FolderPath, "*", SearchOption.AllDirectories);

        project.Files = entries
            .Select(entry => new FileEntry
            {
                Name = entry.Name,
                RelativePath = Path.GetRelativePath(project.FolderPath, entry.FullPath),
                Type = ClassifyEntry(entry)
            })
            .ToList();
    }

    private static FileType ClassifyEntry(FileSystemEntryInfo entry)
    {
        if (entry.IsDirectory)
            return FileType.Directory;

        string extension = Path.GetExtension(entry.FullPath);
        if (extension.Equals(ProjectConstants.TemplateFileExtension, StringComparison.OrdinalIgnoreCase))
            return FileType.Script;

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            if (IsInMetadataFolder(entry.FullPath))
                return FileType.Metadata;
            return FileType.Data;
        }

        return FileType.Other;
    }

    /// <summary>
    /// Determina si una ruta absoluta se encuentra dentro de la carpeta metadata/.
    /// </summary>
    private static bool IsInMetadataFolder(string fullPath)
    {
        string normalized = fullPath.Replace('\\', Path.DirectorySeparatorChar);
        string segment = $"{Path.DirectorySeparatorChar}metadata{Path.DirectorySeparatorChar}";
        return normalized.Contains(segment, StringComparison.OrdinalIgnoreCase);
    }
}
