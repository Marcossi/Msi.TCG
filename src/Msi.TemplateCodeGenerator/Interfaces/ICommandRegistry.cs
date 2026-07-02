namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Registro de comandos. Resuelve comandos por nombre consultando al contexto activo.
/// Actúa como intermediario entre la UI (menú, toolbar, keybindings) y los ViewModels.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Comprueba si el comando especificado puede ejecutarse en el contexto actual.
    /// </summary>
    bool CanExecute(string commandName);

    /// <summary>
    /// Ejecuta el comando especificado en el contexto activo.
    /// </summary>
    /// <returns>true si el comando se ejecutó; false si no hay contexto activo o no puede ejecutarse.</returns>
    Task<bool> ExecuteAsync(string commandName);
}
