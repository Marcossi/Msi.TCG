namespace Msi.TemplateCodeGenerator.Messages;

/// <summary>
/// Mensaje enviado cuando la estructura de archivos del proyecto cambia
/// (crear, eliminar, renombrar, mover, duplicar ficheros o directorios).
/// También enviado por FileWatcherService cuando detecta cambios externos.
/// </summary>
/// <param name="RelativePath">Ruta relativa del fichero cambiado (null si es señal genérica).</param>
/// <param name="ChangeType">Tipo de cambio detectado (null si es señal genérica).</param>
public sealed record ProjectFilesChangedMessage(string? RelativePath = null, Models.FileChangeType? ChangeType = null);
