namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa una entrada (fichero o directorio) dentro de la carpeta de un proyecto.
/// </summary>
public class FileEntry
{
    /// <summary>
    /// Nombre del fichero o directorio.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ruta relativa desde la carpeta raíz del proyecto.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de entrada en el sistema de archivos.
    /// </summary>
    public FileType Type { get; set; }
}
