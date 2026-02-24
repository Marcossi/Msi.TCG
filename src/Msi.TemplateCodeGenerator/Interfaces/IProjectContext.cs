using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Expone el estado del proyecto activo en la aplicación.
/// Este servicio solo contiene datos/estado, sin lógica de operaciones.
/// </summary>
public interface IProjectContext
{
    /// <summary>
    /// Proyecto activo, si existe.
    /// </summary>
    Project? CurrentProject { get; }

    /// <summary>
    /// Ruta del proyecto activo, si existe.
    /// </summary>
    string? CurrentProjectPath { get; }

    /// <summary>
    /// Indica si hay un proyecto abierto.
    /// </summary>
    bool IsProjectOpen => CurrentProject != null;
}
