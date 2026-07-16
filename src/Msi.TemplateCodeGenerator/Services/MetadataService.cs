using System.Text.Json;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Models.Metadata;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Servicio de procesamiento de metadata JSON.
/// Encargado de parsear, cargar defaults y aplicar merge.
/// </summary>
internal sealed class MetadataService(
    IFileSystem fileSystem,
    ILogger<MetadataService> logger) : IMetadataService
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger<MetadataService> _logger = logger;

    /// <inheritdoc/>
    public async Task<MetadataPreviewResult> ProcessPreviewAsync(string jsonContent, string editorFilePath)
    {
        try
        {
            MetadataFile file = JsonSerializer.Deserialize<MetadataFile>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new JsonException("El JSON está vacío o es inválido.");

            string? defaultsFileName = file.Header.Defaults;

            if (!string.IsNullOrEmpty(defaultsFileName) && !string.IsNullOrEmpty(editorFilePath))
            {
                string? directory = Path.GetDirectoryName(editorFilePath);
                if (directory != null)
                {
                    string defaultsPath = Path.Combine(directory, defaultsFileName);

                    if (await _fileSystem.FileExistsAsync(defaultsPath))
                    {
                        string defaultsJson = await _fileSystem.ReadTextAsync(defaultsPath);
                        MetadataFile defaultsFile = JsonSerializer.Deserialize<MetadataFile>(defaultsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? throw new JsonException("El fichero de defaults es inválido.");

                        MergeJsonElements(file.Data, defaultsFile.Data);
                    }
                }
            }

            string preview = JsonSerializer.Serialize(file.Data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return new MetadataPreviewResult
            {
                PreviewContent = preview,
                HasError = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al procesar preview de metadata");
            return new MetadataPreviewResult
            {
                PreviewContent = $"Error: {ex.Message}",
                HasError = true
            };
        }
    }

    /// <summary>
    /// Merge recursivo de dos JsonElement: los valores de source se aplican sobre target
    /// solo cuando target tiene el valor por defecto (null, string vacío, false, 0).
    /// Los arrays no se mergean, solo los objetos.
    /// </summary>
    private static void MergeJsonElements(JsonElement target, JsonElement source)
    {
        if (target.ValueKind != JsonValueKind.Object || source.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty sourceProp in source.EnumerateObject())
        {
            if (!target.TryGetProperty(sourceProp.Name, out JsonElement targetProp))
                continue;

            if (targetProp.ValueKind == JsonValueKind.Object && sourceProp.Value.ValueKind == JsonValueKind.Object)
            {
                MergeJsonElements(targetProp, sourceProp.Value);
            }
        }
    }
}
