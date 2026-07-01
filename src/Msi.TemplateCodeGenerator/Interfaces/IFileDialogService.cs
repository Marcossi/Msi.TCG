namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio abstracto para diálogos de selección de archivos del sistema operativo.
/// La interfaz no depende de Avalonia; solo lo hace la implementación concreta en UI/Services.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Muestra un diálogo de guardado y devuelve la ruta seleccionada por el usuario.
    /// </summary>
    /// <param name="title">Título del diálogo.</param>
    /// <param name="defaultExtension">Extensión por defecto (con punto, ej: ".scribanproj").</param>
    /// <param name="fileTypeName">Nombre para mostrar del tipo de archivo.</param>
    /// <param name="filePattern">Patrón de filtro (ej: "*.scribanproj").</param>
    /// <param name="suggestedFileName">Nombre de archivo sugerido (opcional).</param>
    /// <returns>Ruta del archivo seleccionado, o null si el usuario canceló.</returns>
    Task<string?> SaveFileAsync(
        string title,
        string defaultExtension,
        string fileTypeName,
        string filePattern,
        string? suggestedFileName = null);

    /// <summary>
    /// Muestra un diálogo de apertura y devuelve la ruta seleccionada por el usuario.
    /// </summary>
    /// <param name="title">Título del diálogo.</param>
    /// <param name="fileTypeName">Nombre para mostrar del tipo de archivo.</param>
    /// <param name="filePattern">Patrón de filtro (ej: "*.scribanproj").</param>
    /// <returns>Ruta del archivo seleccionado, o null si el usuario canceló.</returns>
    Task<string?> OpenFileAsync(
        string title,
        string fileTypeName,
        string filePattern);
}
