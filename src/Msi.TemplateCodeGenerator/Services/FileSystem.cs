using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IFileSystem"/> que accede directamente al sistema de ficheros.
/// Es el único punto del código que usa <c>System.IO.File</c> y <c>System.IO.Directory</c> directamente.
/// </summary>
internal sealed class FileSystem(ILogger<FileSystem> logger) : IFileSystem
{
    private readonly ILogger<FileSystem> _logger = logger;

    /// <inheritdoc/>
    public async Task<string> ReadTextAsync(string filePath)
    {
        _logger.LogDebug("Leyendo fichero '{FilePath}'", filePath);
        string content = await File.ReadAllTextAsync(filePath);
        _logger.LogDebug("Fichero leído: '{FilePath}'", filePath);
        return content;
    }

    /// <inheritdoc/>
    public async Task WriteTextAsync(string filePath, string content)
    {
        _logger.LogDebug("Escribiendo fichero '{FilePath}'", filePath);
        await File.WriteAllTextAsync(filePath, content);
        _logger.LogDebug("Fichero escrito: '{FilePath}'", filePath);
    }

    /// <inheritdoc/>
    public async Task CreateFileAsync(string filePath, string content = "")
    {
        _logger.LogDebug("Creando fichero '{FilePath}'", filePath);
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            await Task.Run(() => Directory.CreateDirectory(directory));
        }

        await File.WriteAllTextAsync(filePath, content);
        _logger.LogDebug("Fichero creado: '{FilePath}'", filePath);
    }

    /// <inheritdoc/>
    public async Task CreateDirectoryAsync(string directoryPath)
    {
        _logger.LogDebug("Creando directorio '{DirectoryPath}'", directoryPath);
        await Task.Run(() => Directory.CreateDirectory(directoryPath));
        _logger.LogDebug("Directorio creado: '{DirectoryPath}'", directoryPath);
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string filePath)
    {
        _logger.LogDebug("Eliminando fichero '{FilePath}'", filePath);
        await Task.Run(() => File.Delete(filePath));
        _logger.LogDebug("Fichero eliminado: '{FilePath}'", filePath);
    }

    /// <inheritdoc/>
    public async Task DeleteDirectoryAsync(string directoryPath, bool recursive = false)
    {
        _logger.LogDebug("Eliminando directorio '{DirectoryPath}' (recursive={Recursive})", directoryPath, recursive);
        await Task.Run(() => Directory.Delete(directoryPath, recursive));
        _logger.LogDebug("Directorio eliminado: '{DirectoryPath}'", directoryPath);
    }

    /// <inheritdoc/>
    public async Task MoveFileAsync(string sourcePath, string destinationPath)
    {
        _logger.LogDebug("Moviendo fichero '{Source}' → '{Destination}'", sourcePath, destinationPath);
        await Task.Run(() => File.Move(sourcePath, destinationPath));
        _logger.LogDebug("Fichero movido: '{Source}' → '{Destination}'", sourcePath, destinationPath);
    }

    /// <inheritdoc/>
    public async Task MoveDirectoryAsync(string sourcePath, string destinationPath)
    {
        _logger.LogDebug("Moviendo directorio '{Source}' → '{Destination}'", sourcePath, destinationPath);
        await Task.Run(() => Directory.Move(sourcePath, destinationPath));
        _logger.LogDebug("Directorio movido: '{Source}' → '{Destination}'", sourcePath, destinationPath);
    }

    /// <inheritdoc/>
    public async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        _logger.LogDebug("Copiando fichero '{Source}' → '{Destination}'", sourcePath, destinationPath);
        await Task.Run(() => File.Copy(sourcePath, destinationPath));
        _logger.LogDebug("Fichero copiado: '{Source}' → '{Destination}'", sourcePath, destinationPath);
    }

    /// <inheritdoc/>
    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.Run(() => File.Exists(filePath));
    }

    /// <inheritdoc/>
    public async Task<bool> DirectoryExistsAsync(string directoryPath)
    {
        return await Task.Run(() => Directory.Exists(directoryPath));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EnumerateFilesAsync(string directory, string searchPattern, SearchOption searchOption)
    {
        _logger.LogDebug("Enumerando ficheros en '{Directory}' (pattern={Pattern}, recursive={SearchOption})", directory, searchPattern, searchOption);
        IReadOnlyList<string> result = await Task.Run(() =>
            (IReadOnlyList<string>)Directory.EnumerateFiles(directory, searchPattern, searchOption).ToList());
        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EnumerateDirectoriesAsync(string directory, string searchPattern, SearchOption searchOption)
    {
        _logger.LogDebug("Enumerando directorios en '{Directory}' (pattern={Pattern}, recursive={SearchOption})", directory, searchPattern, searchOption);
        IReadOnlyList<string> result = await Task.Run(() =>
            (IReadOnlyList<string>)Directory.EnumerateDirectories(directory, searchPattern, searchOption).ToList());
        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileSystemEntryInfo>> GetFileSystemInfosAsync(string directory, string searchPattern, SearchOption searchOption)
    {
        _logger.LogDebug("Enumerando entradas en '{Directory}' (pattern={Pattern}, recursive={SearchOption})", directory, searchPattern, searchOption);
        IReadOnlyList<FileSystemEntryInfo> result = await Task.Run(() =>
        {
            List<FileSystemEntryInfo> entries = new DirectoryInfo(directory)
                .EnumerateFileSystemInfos(searchPattern, searchOption)
                .Select(info => new FileSystemEntryInfo(info.FullName, info.Name, info is DirectoryInfo))
                .ToList();
            return (IReadOnlyList<FileSystemEntryInfo>)entries;
        });
        return result;
    }
}
