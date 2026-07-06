namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Resultado del procesamiento de preview de metadata.
/// </summary>
public sealed class MetadataPreviewResult
{
    /// <summary>
    /// Contenido formateado para el preview. Si hubo error, contiene el mensaje de error.
    /// </summary>
    public string PreviewContent { get; init; } = string.Empty;

    /// <summary>
    /// Indica si hubo un error durante el procesamiento.
    /// </summary>
    public bool HasError { get; init; }
}
