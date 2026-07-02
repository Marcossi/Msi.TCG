namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Ruta de comandos contextuales. Los ViewModels que implementan esta interfaz
/// pueden manejar comandos invocados por nombre (ej: "Save", "Copy", "Paste").
/// </summary>
public interface ICommandRoute
{
    /// <summary>
    /// Comprueba si el comando especificado puede ejecutarse en el estado actual.
    /// </summary>
    /// <param name="commandName">Nombre del comando (ej: "Save").</param>
    /// <returns>true si el comando puede ejecutarse; false en caso contrario.</returns>
    bool CanExecute(string commandName);

    /// <summary>
    /// Ejecuta el comando especificado.
    /// </summary>
    /// <param name="commandName">Nombre del comando (ej: "Save").</param>
    /// <exception cref="InvalidOperationException">Si el comando no está soportado o no puede ejecutarse.</exception>
    Task ExecuteAsync(string commandName);
}
