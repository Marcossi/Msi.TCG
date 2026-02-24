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

    // TODO: Futuras propiedades del dominio
    // - Templates (colección de plantillas)
    // - ReferencedAssemblies (ensamblados referenciados)
    // - Configuration (configuración del proyecto)
}
