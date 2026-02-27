namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Parte del servicio que gestiona el FileWatcher del proyecto:
/// registro, respuesta a cambios en disco, parada y limpieza.
/// </summary>
internal sealed partial class ProjectService
{
    // TODO: campo privado FileSystemWatcher

    /// <summary>
    /// Inicia la vigilancia de cambios en la carpeta del proyecto.
    /// </summary>
    private void StartFileWatcher(string folderPath)
    {
        // TODO: Crear y configurar FileSystemWatcher
        // TODO: Suscribirse a Created, Deleted, Renamed
        // TODO: Llamar a ClassifyEntry (disponible en ProjectService.Files.cs) al procesar eventos
    }

    /// <summary>
    /// Detiene y libera el FileWatcher activo.
    /// </summary>
    private void StopFileWatcher()
    {
        // TODO: Detener y hacer Dispose del FileSystemWatcher
    }
}
