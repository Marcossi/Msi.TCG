using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Interfaz interna para mutar el estado del proyecto activo.
/// Solo ProjectService está autorizado a inyectar esta interfaz.
/// </summary>
internal interface IProjectContextMutator
{
    /// <summary>
    /// Establece el proyecto activo y su ruta.
    /// </summary>
    void SetProject(Project project, string projectPath);

    /// <summary>
    /// Actualiza la ruta del proyecto activo (para Save As).
    /// </summary>
    void UpdateProjectPath(string newProjectPath);

    /// <summary>
    /// Limpia el proyecto activo.
    /// </summary>
    void ClearProject();
}
