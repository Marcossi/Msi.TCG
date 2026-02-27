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
}
