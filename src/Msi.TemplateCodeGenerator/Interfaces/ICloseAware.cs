namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contrato para ViewModels que necesitan controlar su propio proceso de cierre.
/// Permite confirmar o vetar el cierre de forma asíncrona antes de que NavigationService
/// ejecute el cierre real en el Dock.
/// </summary>
public interface ICloseAware
{
    /// <summary>
    /// Determina si el ViewModel puede cerrarse.
    /// Puede interactuar con el usuario (p.ej. diálogo de confirmación) antes de decidir.
    /// </summary>
    /// <returns>true si se puede proceder con el cierre; false para abortarlo.</returns>
    Task<bool> CanCloseAsync();
}
