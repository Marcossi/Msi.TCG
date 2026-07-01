using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services.Templates;

namespace Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

/// <summary>
/// ViewModel del editor de plantillas Scriban.
/// Hereda el manejo de fichero y contenido de BaseTextEditorViewModel
/// y añade el renderizado en tiempo real con debounce.
/// </summary>
internal partial class TemplateEditorShellViewModel(
    ITemplatesService templatesService,
    IFileService fileService,
    IDialogService dialogService,
    ILogger<TemplateEditorShellViewModel> logger)
    : BaseTextEditorViewModel(fileService, dialogService, logger)
{
    private readonly ILogger<TemplateEditorShellViewModel> _logger = logger;

    [ObservableProperty]
    private string _previewContent = string.Empty;

    private CancellationTokenSource? _debounceCts;

    /// <inheritdoc/>
    protected override void OnContentChangedCore(string value)
    {
        _logger.LogDebug("Contenido cambiado ({CharCount} chars), reiniciando debounce", value.Length);

        // Cancelar la ejecución pendiente anterior (si el usuario sigue escribiendo)
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        // Iniciar el proceso de actualización con debounce (Fire-and-forget seguro)
        _ = UpdatePreviewWithDebounceAsync(value, token);
    }

    private async Task UpdatePreviewWithDebounceAsync(string content, CancellationToken token)
    {
        try
        {
            // Esperar 1 segundo de inactividad (Debounce)
            await Task.Delay(1000, token);

            if (token.IsCancellationRequested)
            {
                _logger.LogDebug("Debounce cancelado por nuevo cambio de contenido");
                return;
            }

            // Llamada al servicio de transformación
            _logger.LogDebug("Renderizando preview con debounce");
            TemplateResult result = await templatesService.ProcessTemplateAsync(content);

            if (token.IsCancellationRequested)
                return;

            // Actualizar la propiedad solo si la tarea sigue siendo válida
            if (result.IsSuccess)
            {
                PreviewContent = result.Result;
                StatusMessage = "Preview actualizado correctamente.";
                _logger.LogDebug("Preview actualizado exitosamente");
            }
            else
            {
                PreviewContent = $"Error: {result.ErrorMessage}";
                StatusMessage = $"Error: {result.ErrorMessage}";
                _logger.LogWarning("Error en preview: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (TaskCanceledException)
        {
            // La tarea fue cancelada porque el usuario escribió algo nuevo. Ignoramos.
        }
    }
}
