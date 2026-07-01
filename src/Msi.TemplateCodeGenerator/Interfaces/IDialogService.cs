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
}
