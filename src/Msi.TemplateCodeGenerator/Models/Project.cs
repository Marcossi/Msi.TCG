namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa un proyecto de plantillas de código.
/// Contiene toda la información del dominio relacionada con el proyecto.
/// </summary>
public class Project
{
    /// <summary>
    /// Nombre del proyecto.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ruta en disco de la carpeta raíz del proyecto.
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Ficheros y directorios que pertenecen al proyecto.  
    /// Se actualiza mediante la operación de refresco del servicio.
    /// </summary>
    public List<FileEntry> Files { get; set; } = [];

    // TODO: Futuras propiedades del dominio
    // - ReferencedAssemblies (ensamblados referenciados)
    // - Configuration (configuración del proyecto)
}
