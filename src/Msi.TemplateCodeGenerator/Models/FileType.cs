namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Tipo de entrada en el sistema de archivos del proyecto.
/// </summary>
public enum FileType
{
    /// <summary>
    /// Nodo raíz que representa el proyecto en sí.
    /// </summary>
    Project,

    /// <summary>
    /// Plantilla Scriban (.scriban).
    /// </summary>
    Script,

    /// <summary>
    /// Fichero JSON de metadatos dentro de la carpeta metadata/.
    /// </summary>
    Metadata,

    /// <summary>
    /// Directorio.
    /// </summary>
    Directory,

    /// <summary>
    /// Otro tipo de archivo no reconocido.
    /// </summary>
    Other
}
