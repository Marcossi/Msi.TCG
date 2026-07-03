namespace Msi.TemplateCodeGenerator.Models.Metadata;

/// <summary>
/// Cabecera de un fichero de metadatos.
/// Contiene información de infraestructura (versión, categoría, referencia a defaults).
/// </summary>
public sealed class MetadataFileHeader
{
    public int Version { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Defaults { get; set; }
}
