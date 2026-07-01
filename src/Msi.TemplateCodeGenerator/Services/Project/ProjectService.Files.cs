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
    public Task RefreshFilesAsync()
    {
        if (!_context.IsProjectOpen)
            throw new InvalidOperationException("No project is currently open.");

        Models.Project project = _context.CurrentProject!;

        if (string.IsNullOrWhiteSpace(project.FolderPath) || !Directory.Exists(project.FolderPath))
            throw new InvalidOperationException("La carpeta del proyecto no existe o su ruta no está configurada.");

        project.Files = new DirectoryInfo(project.FolderPath)
            .EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
            .Select(entry => new FileEntry
            {
                Name = entry.Name,
                RelativePath = Path.GetRelativePath(project.FolderPath, entry.FullName),
                Type = ClassifyEntry(entry)
            })
            .ToList();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clasifica una entrada del sistema de archivos en su <see cref="FileType"/> correspondiente.
    /// </summary>
    /// <param name="entry">Entrada del sistema de archivos a clasificar.</param>
    /// <returns>El tipo de fichero determinado para la entrada.</returns>
    private static FileType ClassifyEntry(FileSystemInfo entry)
    {
        if (entry is DirectoryInfo)
            return FileType.Directory;

        if (entry is FileInfo file &&
            file.Extension.Equals(ProjectConstants.TemplateFileExtension, StringComparison.OrdinalIgnoreCase))
            return FileType.Script;

        return FileType.Other;
    }
}
