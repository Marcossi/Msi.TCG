using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contrato de infraestructura unificado para operaciones de fichero y directorio.
/// Abstrae el acceso al sistema de ficheros para facilitar las pruebas y el desacoplamiento.
/// La implementación (<c>FileSystem</c>) es el único punto que accede a <c>System.IO</c>.
/// </summary>
public interface IFileSystem
{
    Task<string> ReadTextAsync(string filePath);

    Task WriteTextAsync(string filePath, string content);

    Task CreateFileAsync(string filePath, string content = "");

    Task CreateDirectoryAsync(string directoryPath);

    Task DeleteFileAsync(string filePath);

    Task DeleteDirectoryAsync(string directoryPath, bool recursive = false);

    Task MoveFileAsync(string sourcePath, string destinationPath);

    Task MoveDirectoryAsync(string sourcePath, string destinationPath);

    Task CopyFileAsync(string sourcePath, string destinationPath);

    Task<bool> FileExistsAsync(string filePath);

    Task<bool> DirectoryExistsAsync(string directoryPath);

    /// <summary>
    /// Enumera los ficheros de un directorio que coinciden con el patrón de búsqueda.
    /// </summary>
    Task<IReadOnlyList<string>> EnumerateFilesAsync(string directory, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Enumera los subdirectorios de un directorio que coinciden con el patrón de búsqueda.
    /// </summary>
    Task<IReadOnlyList<string>> EnumerateDirectoriesAsync(string directory, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Enumera todas las entradas del sistema de ficheros (ficheros y directorios)
    /// de un directorio que coinciden con el patrón de búsqueda.
    /// </summary>
    Task<IReadOnlyList<FileSystemEntryInfo>> GetFileSystemInfosAsync(string directory, string searchPattern, SearchOption searchOption);
}
