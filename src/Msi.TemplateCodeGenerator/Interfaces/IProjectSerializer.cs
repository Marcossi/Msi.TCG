using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Interfaz para serializar/deserializar proyectos a disco.
/// Abstrae el formato (JSON, XML, YAML, etc.) de persistencia.
/// </summary>
public interface IProjectSerializer
{
    /// <summary>
    /// Guarda un proyecto en disco.
    /// </summary>
    /// <param name="project">Proyecto a guardar.</param>
    /// <param name="filePath">Ruta completa del archivo.</param>
    Task SaveAsync(Project project, string filePath);

    /// <summary>
    /// Carga un proyecto desde disco.
    /// </summary>
    /// <param name="filePath">Ruta completa del archivo.</param>
    /// <returns>Proyecto cargado.</returns>
    Task<Project> LoadAsync(string filePath);
}
