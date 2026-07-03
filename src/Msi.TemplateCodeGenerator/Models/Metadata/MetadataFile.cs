using System.Text.Json;

namespace Msi.TemplateCodeGenerator.Models.Metadata;

/// <summary>
/// Wrapper para la deserialización de un fichero de metadatos completo.
/// Separa la cabecera de infraestructura (header) de los datos de dominio (data).
/// Data se mantiene como JsonElement para permitir deserialización dinámica posterior según la categoría.
/// </summary>
public sealed class MetadataFile
{
    public MetadataFileHeader Header { get; set; } = new();
    public JsonElement Data { get; set; }
}
