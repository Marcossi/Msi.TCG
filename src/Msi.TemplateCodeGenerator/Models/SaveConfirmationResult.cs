namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Resultado de la confirmación de guardado al intentar cerrar un documento con cambios pendientes.
/// </summary>
public enum SaveConfirmationResult
{
    /// <summary>El usuario eligió guardar los cambios antes de cerrar.</summary>
    Save,

    /// <summary>El usuario eligió cerrar sin guardar los cambios.</summary>
    DontSave,

    /// <summary>El usuario canceló la operación de cierre.</summary>
    Cancel
}
