namespace Msi.TemplateCodeGenerator.Constants;

/// <summary>
/// Constantes relacionadas con proyectos y archivos.
/// </summary>
public static class ProjectConstants
{
    /// <summary>
    /// Extensión de archivo de proyecto (incluye el punto).
    /// </summary>
    public const string ProjectFileExtension = ".scribanproj";

    /// <summary>
    /// Extensión de archivo de plantilla (incluye el punto).
    /// </summary>
    public const string TemplateFileExtension = ".scriban";

    /// <summary>
    /// Filtro para diálogos de archivo (solo extensión sin punto).
    /// </summary>
    public const string ProjectFilePattern = "*.scribanproj";

    /// <summary>
    /// Nombre para mostrar en diálogos de archivo.
    /// </summary>
    public const string ProjectFileTypeName = "Proyecto de Plantillas Scriban";

    /// <summary>
    /// Versión actual del formato de archivo de proyecto.
    /// Incrementar cuando se hagan cambios incompatibles en el esquema.
    /// </summary>
    public const int CurrentFileFormatVersion = 1;

    /// <summary>
    /// Versión mínima del formato de archivo soportada para lectura.
    /// Archivos con versión menor requerirán migración.
    /// </summary>
    public const int MinimumSupportedFileFormatVersion = 1;
}
