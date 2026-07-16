namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Resultado de la ejecución de un script Scriban.
/// </summary>
public sealed class ScriptExecutionResult
{
    /// <summary>
    /// Indica si la ejecución fue exitosa.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Mensajes de error (si Success es false).
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Outputs generados por write_to_file.
    /// </summary>
    public IReadOnlyList<ScriptOutput> Outputs { get; init; } = [];

    /// <summary>
    /// Contenido renderizado del template (texto + interpolaciones).
    /// </summary>
    public string RenderedContent { get; init; } = string.Empty;
}

/// <summary>
/// Representa un output generado por write_to_file.
/// </summary>
public sealed class ScriptOutput
{
    /// <summary>
    /// Ruta relativa del fichero generado.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Contenido generado.
    /// </summary>
    public string Content { get; init; } = string.Empty;
}
