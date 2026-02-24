using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Implementación del contexto del proyecto activo.
/// Solo almacena el estado actual, sin lógica de operaciones.
/// </summary>
internal sealed class ProjectContext : IProjectContext
{
    private Models.Project? _currentProject;
    private string? _currentProjectPath;

    /// <summary>
    /// Proyecto activo, si existe.
    /// </summary>
    public Models.Project? CurrentProject
    {
        get => _currentProject;
        internal set => _currentProject = value;
    }

    /// <summary>
    /// Ruta del proyecto activo, si existe.
    /// </summary>
    public string? CurrentProjectPath
    {
        get => _currentProjectPath;
        internal set => _currentProjectPath = value;
    }

    /// <summary>
    /// Indica si hay un proyecto abierto.
    /// </summary>
    public bool IsProjectOpen => CurrentProject != null;
}
