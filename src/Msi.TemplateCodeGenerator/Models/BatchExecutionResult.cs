namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Resultado agregado de la ejecución batch de múltiples scripts.
/// </summary>
public sealed class BatchExecutionResult
{
    /// <summary>
    /// Número de scripts ejecutados exitosamente.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Número de scripts que fallaron.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Mensajes de error de los scripts que fallaron.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
