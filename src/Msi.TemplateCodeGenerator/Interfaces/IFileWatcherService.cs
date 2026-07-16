namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio de vigilancia de cambios en ficheros del proyecto.
/// Publica <see cref="Messages.ProjectFilesChangedMessage"/> via IMessenger cuando detecta cambios.
/// </summary>
public interface IFileWatcherService
{
    /// <summary>
    /// Inicia la vigilancia en un directorio.
    /// </summary>
    void StartWatching(string directoryPath);

    /// <summary>
    /// Detiene la vigilancia.
    /// </summary>
    void StopWatching();
}
