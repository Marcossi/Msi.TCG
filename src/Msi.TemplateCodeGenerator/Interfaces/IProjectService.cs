using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio que gestiona las operaciones relacionadas con proyectos:
/// apertura, cierre, carga, guardado, validación, FileWatcher, etc.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Abre un proyecto desde la ruta especificada.
    /// </summary>
    Task OpenProjectAsync(string projectPath);

    /// <summary>
    /// Cierra el proyecto activo.
    /// </summary>
    Task CloseProjectAsync();

    /// <summary>
    /// Guarda el proyecto actual en disco.
    /// </summary>
    Task SaveProjectAsync();

    /// <summary>
    /// Guarda el proyecto actual en una nueva ubicación.
    /// </summary>
    Task SaveProjectAsAsync(string newProjectPath);

    /// <summary>
    /// Crea un nuevo proyecto en la ruta especificada.
    /// </summary>
    Task CreateNewProjectAsync(string projectPath, string projectName);

    /// <summary>
    /// Refresca la lista de ficheros del proyecto activo
    /// escaneando la carpeta raíz del proyecto en disco.
    /// </summary>
    Task RefreshFilesAsync();

    /// <summary>
    /// Crea un nuevo fichero dentro del directorio especificado del proyecto activo.
    /// </summary>
    /// <param name="parentRelativePath">Ruta relativa del directorio padre (vacía para raíz).</param>
    /// <param name="fileName">Nombre del fichero a crear.</param>
    /// <returns>La entrada de fichero creada.</returns>
    Task<FileEntry> CreateFileAsync(string parentRelativePath, string fileName);

    /// <summary>
    /// Crea un nuevo directorio dentro del directorio especificado del proyecto activo.
    /// </summary>
    /// <param name="parentRelativePath">Ruta relativa del directorio padre (vacía para raíz).</param>
    /// <param name="directoryName">Nombre del directorio a crear.</param>
    /// <returns>La entrada de directorio creada.</returns>
    Task<FileEntry> CreateDirectoryAsync(string parentRelativePath, string directoryName);

    /// <summary>
    /// Renombra un fichero o directorio del proyecto activo.
    /// </summary>
    /// <param name="relativePath">Ruta relativa del elemento a renombrar.</param>
    /// <param name="newName">Nuevo nombre.</param>
    Task RenameAsync(string relativePath, string newName);

    /// <summary>
    /// Elimina un fichero o directorio del proyecto activo.
    /// </summary>
    /// <param name="relativePath">Ruta relativa del elemento a eliminar.</param>
    Task DeleteAsync(string relativePath);

    /// <summary>
    /// Duplica un fichero o directorio del proyecto activo.
    /// </summary>
    /// <param name="relativePath">Ruta relativa del elemento a duplicar.</param>
    /// <returns>La entrada de fichero duplicada.</returns>
    Task<FileEntry> DuplicateAsync(string relativePath);

    /// <summary>
    /// Mueve un fichero o directorio a otro directorio dentro del proyecto activo.
    /// </summary>
    /// <param name="sourceRelativePath">Ruta relativa del elemento a mover.</param>
    /// <param name="targetParentRelativePath">Ruta relativa del directorio destino.</param>
    Task MoveAsync(string sourceRelativePath, string targetParentRelativePath);
}
