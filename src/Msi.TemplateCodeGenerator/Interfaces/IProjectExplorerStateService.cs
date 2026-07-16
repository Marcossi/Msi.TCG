using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio para persistir y restaurar el estado de UI del explorador de proyectos.
/// </summary>
public interface IProjectExplorerStateService
{
    /// <summary>
    /// Guarda el estado de UI para el proyecto indicado.
    /// </summary>
    /// <param name="projectPath">Ruta del fichero de proyecto.</param>
    /// <param name="state">Estado a persistir.</param>
    Task SaveStateAsync(string projectPath, ProjectExplorerState state);

    /// <summary>
    /// Carga el estado de UI previamente guardado para el proyecto indicado.
    /// </summary>
    /// <param name="projectPath">Ruta del fichero de proyecto.</param>
    /// <returns>El estado guardado, o null si no existe estado previo.</returns>
    Task<ProjectExplorerState?> LoadStateAsync(string projectPath);

    /// <summary>
    /// Asegura que los directorios del editor existen en la carpeta del proyecto.
    /// Crea la estructura .tcg/state/ si no existe.
    /// </summary>
    /// <param name="projectPath">Ruta del fichero de proyecto.</param>
    Task EnsureEditorDirectoriesExistAsync(string projectPath);
}
