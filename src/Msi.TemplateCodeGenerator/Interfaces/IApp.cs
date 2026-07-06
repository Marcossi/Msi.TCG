namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Fachada global para operaciones de la shell.
/// Expone únicamente lo que los ViewModels realmente necesitan.
/// </summary>
public interface IApp
{
    /// <summary>
    /// Cierra la aplicación de forma controlada.
    /// </summary>
    void Shutdown();
}
