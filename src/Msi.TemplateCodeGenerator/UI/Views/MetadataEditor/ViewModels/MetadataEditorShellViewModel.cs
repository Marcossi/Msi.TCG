using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models.Metadata;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Views.MetadataEditor.ViewModels;

/// <summary>
/// ViewModel del editor de metadatos JSON.
/// Hereda el manejo de fichero y contenido de BaseTextEditorViewModel
/// y añade la validación JSON y el preview del objeto parseado con defaults aplicados.
/// </summary>
internal sealed partial class MetadataEditorShellViewModel(
    IFileService fileService,
    IDialogService dialogService,
    ILogger<MetadataEditorShellViewModel> logger)
    : BaseTextEditorViewModel(fileService, dialogService, logger)
{
    private readonly ILogger<MetadataEditorShellViewModel> _logger = logger;

    [ObservableProperty]
    private string _previewContent = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    private CancellationTokenSource? _debounceCts;

    /// <inheritdoc/>
    protected override void OnContentChangedCore(string value)
    {
        _logger.LogDebug("Contenido JSON cambiado ({CharCount} chars), reiniciando debounce", value.Length);

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        _ = UpdatePreviewWithDebounceAsync(value, token);
    }

    private async Task UpdatePreviewWithDebounceAsync(string content, CancellationToken token)
    {
        try
        {
            await Task.Delay(500, token);

            if (token.IsCancellationRequested)
            {
                _logger.LogDebug("Debounce cancelado por nuevo cambio de contenido");
                return;
            }

            _logger.LogDebug("Actualizando preview de metadata");
            UpdatePreview(content);

            if (token.IsCancellationRequested)
                return;

            StatusMessage = HasError ? "Error en JSON" : "Preview actualizado correctamente.";
        }
        catch (TaskCanceledException)
        {
        }
    }

    /// <summary>
    /// Parsea el JSON, carga defaults si existen, aplica merge y actualiza el preview.
    /// </summary>
    private void UpdatePreview(string jsonContent)
    {
        try
        {
            MetadataFile file = JsonSerializer.Deserialize<MetadataFile>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new JsonException("El JSON está vacío o es inválido.");

            string? defaultsFileName = file.Header.Defaults;

            if (!string.IsNullOrEmpty(defaultsFileName) && !string.IsNullOrEmpty(FilePath))
            {
                string? directory = Path.GetDirectoryName(FilePath);
                if (directory != null)
                {
                    string defaultsPath = Path.Combine(directory, defaultsFileName);

                    if (File.Exists(defaultsPath))
                    {
                        string defaultsJson = File.ReadAllText(defaultsPath);
                        MetadataFile defaultsFile = JsonSerializer.Deserialize<MetadataFile>(defaultsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? throw new JsonException("El fichero de defaults es inválido.");

                        MergeJsonElements(file.Data, defaultsFile.Data);
                    }
                }
            }

            PreviewContent = JsonSerializer.Serialize(file.Data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            HasError = false;
            _logger.LogDebug("Preview de metadata actualizado correctamente");
        }
        catch (Exception ex)
        {
            PreviewContent = $"Error: {ex.Message}";
            HasError = true;
            _logger.LogWarning(ex, "Error al parsear JSON de metadata");
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
