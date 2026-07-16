using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Parte del servicio que gestiona las operaciones de dominio sobre ficheros y directorios
/// del proyecto activo: crear, renombrar, eliminar, duplicar y mover.
/// </summary>
internal sealed partial class ProjectService
{
    /// <inheritdoc/>
    public async Task<FileEntry> CreateFileAsync(string parentRelativePath, string fileName)
    {
        EnsureProjectOpen();
        ValidateFileName(fileName);

        Models.Project project = _context.CurrentProject!;
        string parentPath = NormalizePath(parentRelativePath);
        string relativePath = string.IsNullOrEmpty(parentPath)
            ? fileName
            : $"{parentPath}/{fileName}";
        string fullPath = Path.Combine(project.FolderPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (await _fileSystem.FileExistsAsync(fullPath))
        {
            throw new InvalidOperationException($"El fichero '{relativePath}' ya existe.");
        }

        _logger.LogInformation("[UI] Command: CreateFile '{RelativePath}'", relativePath);

        await _fileSystem.CreateFileAsync(fullPath);
        await RefreshFilesAsync();
        await SaveProjectAsync();
        _messenger.Send(new ProjectFilesChangedMessage());

        return FindFileEntry(relativePath)
            ?? throw new InvalidOperationException($"No se encontró la entrada '{relativePath}' tras el refresco.");
    }

    /// <inheritdoc/>
    public async Task<FileEntry> CreateDirectoryAsync(string parentRelativePath, string directoryName)
    {
        EnsureProjectOpen();
        ValidateDirectoryName(directoryName);

        Models.Project project = _context.CurrentProject!;
        string parentPath = NormalizePath(parentRelativePath);
        string relativePath = string.IsNullOrEmpty(parentPath)
            ? directoryName
            : $"{parentPath}/{directoryName}";
        string fullPath = Path.Combine(project.FolderPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (await _fileSystem.DirectoryExistsAsync(fullPath))
        {
            throw new InvalidOperationException($"El directorio '{relativePath}' ya existe.");
        }

        _logger.LogInformation("[UI] Command: CreateDirectory '{RelativePath}'", relativePath);

        await _fileSystem.CreateDirectoryAsync(fullPath);
        await RefreshFilesAsync();
        await SaveProjectAsync();
        _messenger.Send(new ProjectFilesChangedMessage());

        return FindFileEntry(relativePath)
            ?? throw new InvalidOperationException($"No se encontró la entrada '{relativePath}' tras el refresco.");
    }

    /// <inheritdoc/>
    public async Task RenameAsync(string relativePath, string newName)
    {
        EnsureProjectOpen();

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(newName));
        }

        Models.Project project = _context.CurrentProject!;
        string normalizedPath = NormalizePath(relativePath);
        string fullPath = Path.Combine(project.FolderPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

        bool isDirectory = await _fileSystem.DirectoryExistsAsync(fullPath);
        if (isDirectory)
        {
            ValidateDirectoryName(newName);
        }
        else
        {
            ValidateFileName(newName);
        }

        string parentPath = normalizedPath.Contains('/')
            ? normalizedPath[..normalizedPath.LastIndexOf('/')]
            : "";
        string newRelativePath = string.IsNullOrEmpty(parentPath)
            ? newName
            : $"{parentPath}/{newName}";
        string newFullPath = Path.Combine(project.FolderPath, newRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (await (isDirectory ? _fileSystem.DirectoryExistsAsync(newFullPath) : _fileSystem.FileExistsAsync(newFullPath)))
        {
            throw new InvalidOperationException($"Ya existe un elemento con el nombre '{newName}' en esa ubicación.");
        }

        _logger.LogInformation("[UI] Command: Rename '{OldPath}' → '{NewName}'", relativePath, newName);

        if (isDirectory)
        {
            await _fileSystem.MoveDirectoryAsync(fullPath, newFullPath);
        }
        else
        {
            await _fileSystem.MoveFileAsync(fullPath, newFullPath);
        }

        await RefreshFilesAsync();
        await SaveProjectAsync();
        _messenger.Send(new ProjectFilesChangedMessage());
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string relativePath)
    {
        EnsureProjectOpen();

        Models.Project project = _context.CurrentProject!;
        string normalizedPath = NormalizePath(relativePath);
        string fullPath = Path.Combine(project.FolderPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

        bool isDirectory = await _fileSystem.DirectoryExistsAsync(fullPath);

        _logger.LogInformation("[UI] Command: Delete '{RelativePath}'", relativePath);

        if (isDirectory)
        {
            await _fileSystem.DeleteDirectoryAsync(fullPath, recursive: true);
        }
        else
        {
            await _fileSystem.DeleteFileAsync(fullPath);
        }

        await RefreshFilesAsync();
        await SaveProjectAsync();
        _messenger.Send(new ProjectFilesChangedMessage());
    }

    /// <inheritdoc/>
    public async Task<FileEntry> DuplicateAsync(string relativePath)
    {
        EnsureProjectOpen();

        Models.Project project = _context.CurrentProject!;
        string normalizedPath = NormalizePath(relativePath);
        string fullPath = Path.Combine(project.FolderPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

        bool isDirectory = await _fileSystem.DirectoryExistsAsync(fullPath);
        string copyName = await GetNextCopyNameAsync(project, normalizedPath, isDirectory);

        string parentPath = normalizedPath.Contains('/')
            ? normalizedPath[..normalizedPath.LastIndexOf('/')]
            : "";
        string copyRelativePath = string.IsNullOrEmpty(parentPath)
            ? copyName
            : $"{parentPath}/{copyName}";
        string copyFullPath = Path.Combine(project.FolderPath, copyRelativePath.Replace('/', Path.DirectorySeparatorChar));

        _logger.LogInformation("[UI] Command: Duplicate '{RelativePath}' → '{CopyPath}'", relativePath, copyRelativePath);

        if (isDirectory)
        {
            await CopyDirectoryRecursiveAsync(fullPath, copyFullPath);
        }
        else
        {
            await _fileSystem.CopyFileAsync(fullPath, copyFullPath);
        }

        await RefreshFilesAsync();
        await SaveProjectAsync();
        _messenger.Send(new ProjectFilesChangedMessage());

        return FindFileEntry(copyRelativePath)
            ?? throw new InvalidOperationException($"No se encontró la entrada '{copyRelativePath}' tras el refresco.");
    }

    /// <inheritdoc/>
    public async Task MoveAsync(string sourceRelativePath, string targetParentRelativePath)
    {
        EnsureProjectOpen();

        Models.Project project = _context.CurrentProject!;
        string normalizedSource = NormalizePath(sourceRelativePath);
        string normalizedTarget = NormalizePath(targetParentRelativePath);

        string sourceFullPath = Path.Combine(project.FolderPath, normalizedSource.Replace('/', Path.DirectorySeparatorChar));
        bool isDirectory = await _fileSystem.DirectoryExistsAsync(sourceFullPath);

        string entryName = Path.GetFileName(normalizedSource.Replace('/', Path.DirectorySeparatorChar));
        string destRelativePath = string.IsNullOrEmpty(normalizedTarget)
            ? entryName
            : $"{normalizedTarget}/{entryName}";
        string destFullPath = Path.Combine(project.FolderPath, destRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (await (isDirectory ? _fileSystem.DirectoryExistsAsync(destFullPath) : _fileSystem.FileExistsAsync(destFullPath)))
        {
            throw new InvalidOperationException($"Ya existe un elemento '{entryName}' en el directorio destino.");
        }

        _logger.LogInformation("[UI] Command: Move '{Source}' → '{Target}'", sourceRelativePath, targetParentRelativePath);

        if (isDirectory)
        {
            await _fileSystem.MoveDirectoryAsync(sourceFullPath, destFullPath);
        }
        else
        {
            await _fileSystem.MoveFileAsync(sourceFullPath, destFullPath);
        }

        await RefreshFilesAsync();
        await SaveProjectAsync();
        _messenger.Send(new ProjectFilesChangedMessage());
    }

    private void EnsureProjectOpen()
    {
        if (!_context.IsProjectOpen)
        {
            throw new InvalidOperationException("No hay ningún proyecto abierto.");
        }
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("El nombre del fichero no puede estar vacío.", nameof(fileName));
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException($"El nombre '{fileName}' contiene caracteres no válidos.", nameof(fileName));
        }
    }

    private static void ValidateDirectoryName(string directoryName)
    {
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            throw new ArgumentException("El nombre del directorio no puede estar vacío.", nameof(directoryName));
        }

        char[] invalidChars = Path.GetInvalidPathChars();
        if (directoryName.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException($"El nombre '{directoryName}' contiene caracteres no válidos.", nameof(directoryName));
        }
    }

    private static string NormalizePath(string path)
    {
        return (path ?? "").Replace('\\', '/').Trim('/');
    }

    private FileEntry? FindFileEntry(string relativePath)
    {
        return _context.CurrentProject?.Files
            .FirstOrDefault(f => NormalizePath(f.RelativePath).Equals(relativePath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GetNextCopyNameAsync(Models.Project project, string relativePath, bool isDirectory)
    {
        string folderPath = Path.GetDirectoryName(relativePath.Replace('/', Path.DirectorySeparatorChar)) ?? "";
        string nameWithoutExt = isDirectory
            ? Path.GetFileName(relativePath.Replace('/', Path.DirectorySeparatorChar))
            : Path.GetFileNameWithoutExtension(relativePath.Replace('/', Path.DirectorySeparatorChar));
        string extension = isDirectory ? "" : Path.GetExtension(relativePath.Replace('/', Path.DirectorySeparatorChar));

        for (int i = 0; i < 1000; i++)
        {
            string suffix = i == 0 ? "_copy" : $"_copy{i}";
            string candidateName = $"{nameWithoutExt}{suffix}{extension}";
            string candidateRelative = string.IsNullOrEmpty(folderPath)
                ? candidateName
                : $"{folderPath.Replace('\\', '/')}/{candidateName}";
            string candidateFull = Path.Combine(
                project.FolderPath,
                candidateRelative.Replace('/', Path.DirectorySeparatorChar));

            bool exists = isDirectory
                ? await _fileSystem.DirectoryExistsAsync(candidateFull)
                : await _fileSystem.FileExistsAsync(candidateFull);

            if (!exists)
            {
                return candidateName;
            }
        }

        throw new InvalidOperationException("No se pudo generar un nombre único para la copia.");
    }

    private async Task CopyDirectoryRecursiveAsync(string sourceDir, string destDir)
    {
        await _fileSystem.CreateDirectoryAsync(destDir);

        foreach (string file in await _fileSystem.EnumerateFilesAsync(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            await _fileSystem.CopyFileAsync(file, destFile);
        }

        foreach (string subDir in await _fileSystem.EnumerateDirectoriesAsync(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            await CopyDirectoryRecursiveAsync(subDir, destSubDir);
        }
    }
}
