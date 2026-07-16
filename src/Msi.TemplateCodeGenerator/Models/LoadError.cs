namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa un error al cargar un fichero JSON.
/// </summary>
public sealed class LoadError
{
    /// <summary>
    /// Ruta del fichero que causó el error.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje de error.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Excepción original (si aplica).
    /// </summary>
    public Exception? Exception { get; set; }
}
