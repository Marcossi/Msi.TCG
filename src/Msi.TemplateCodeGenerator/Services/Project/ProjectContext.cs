using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Implementación del contexto del proyecto activo.
/// Solo almacena el estado actual, sin lógica de operaciones.
/// Implementa IProjectContext (read-only) e IProjectContextMutator (mutación interna).
/// </summary>
internal sealed class ProjectContext : IProjectContext, IProjectContextMutator
{
    private Models.Project? _currentProject;
    private string? _currentProjectPath;

    /// <inheritdoc/>
    public Models.Project? CurrentProject
    {
        get => _currentProject;
        private set => _currentProject = value;
    }

    /// <inheritdoc/>
    public string? CurrentProjectPath
    {
        get => _currentProjectPath;
        private set => _currentProjectPath = value;
    }

    /// <inheritdoc/>
    void IProjectContextMutator.SetProject(Models.Project project, string projectPath)
    {
        CurrentProject = project;
        CurrentProjectPath = projectPath;
    }

    /// <inheritdoc/>
    void IProjectContextMutator.UpdateProjectPath(string newProjectPath)
    {
        CurrentProjectPath = newProjectPath;
    }

    /// <inheritdoc/>
    void IProjectContextMutator.ClearProject()
    {
        CurrentProject = null;
        CurrentProjectPath = null;
    }
}
