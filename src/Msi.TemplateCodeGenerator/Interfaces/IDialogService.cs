using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contrato para mostrar diálogos de confirmación al usuario.
/// La interfaz no depende de Avalonia; solo lo hace la implementación concreta en UI/Services.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Muestra un diálogo preguntando al usuario si desea guardar cambios antes de cerrar.
    /// </summary>
    /// <param name="fileName">Nombre del fichero con cambios sin guardar.</param>
    Task<SaveConfirmationResult> ShowSaveConfirmationAsync(string fileName);

    /// <summary>
    /// Muestra un diálogo de información al usuario.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="title">Título del diálogo.</param>
    Task ShowInfoAsync(string message, string title);

    /// <summary>
    /// Muestra un diálogo de error al usuario.
    /// </summary>
    /// <param name="message">Mensaje de error.</param>
    /// <param name="title">Título del diálogo.</param>
    Task ShowErrorAsync(string message, string title);

    /// <summary>
    /// Muestra un diálogo de advertencia al usuario.
    /// </summary>
    /// <param name="message">Mensaje de advertencia.</param>
    /// <param name="title">Título del diálogo.</param>
    Task ShowWarningAsync(string message, string title);

    /// <summary>
    /// Muestra un diálogo de confirmación genérico con botones Sí/No.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="title">Título del diálogo.</param>
    /// <returns>True si el usuario confirmó; false en caso contrario.</returns>
    Task<bool> ShowConfirmationAsync(string message, string title);
}
