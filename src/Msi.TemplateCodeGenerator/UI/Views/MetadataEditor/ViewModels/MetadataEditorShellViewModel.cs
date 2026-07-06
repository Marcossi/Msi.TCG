using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Views.MetadataEditor.ViewModels;

/// <summary>
/// ViewModel del editor de metadatos JSON.
/// Hereda el manejo de fichero y contenido de BaseTextEditorViewModel
/// y delega el procesamiento de metadata en IMetadataService.
/// </summary>
internal sealed partial class MetadataEditorShellViewModel(
    IFileService fileService,
    IDialogService dialogService,
    IMetadataService metadataService,
    ILogger<MetadataEditorShellViewModel> logger)
    : BaseTextEditorViewModel(fileService, dialogService, logger)
{
    private readonly ILogger<MetadataEditorShellViewModel> _logger = logger;
    private readonly IMetadataService _metadataService = metadataService;

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

            MetadataPreviewResult result = await _metadataService.ProcessPreviewAsync(content, FilePath);

            if (token.IsCancellationRequested)
                return;

            PreviewContent = result.PreviewContent;
            HasError = result.HasError;
            StatusMessage = HasError ? "Error en JSON" : "Preview actualizado correctamente.";
        }
        catch (TaskCanceledException)
        {
        }
    }
}
