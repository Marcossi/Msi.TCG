using CommunityToolkit.Mvvm.ComponentModel;
using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.UI.TemplateEditor;

internal partial class TemplateEditorShellViewModel(ITemplatesService templatesService) : BaseViewModel
{
    [ObservableProperty]
    private string _statusMessage = "Hello World! From: Template Editor ShellViewModel!";

    [ObservableProperty]
    private string _templateContent = string.Empty;

    [ObservableProperty]
    private string _previewContent = string.Empty;

    private CancellationTokenSource? _debounceCts;

    /// <summary>
    /// Método parcial generado por CommunityToolkit.Mvvm que se ejecuta cuando cambia TemplateContent.
    /// </summary>
    partial void OnTemplateContentChanged(string value)
    {
        // 1. Cancelar la ejecución pendiente anterior (si el usuario sigue escribiendo)
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        // 2. Iniciar el proceso de actualización con debounce (Fire-and-forget seguro)
        _ = UpdatePreviewWithDebounceAsync(value, token);
    }

    private async Task UpdatePreviewWithDebounceAsync(string content, CancellationToken token)
    {
        try
        {
            // Esperar 1 segundo de inactividad (Debounce)
            await Task.Delay(1000, token);

            // Si se canceló durante la espera, salimos
            if (token.IsCancellationRequested)
                return;

            // Llamada al servicio de transformación
            var result = await templatesService.ProcessTemplateAsync(content);

            // Si se cancelo durante la ejecución del servicio, salimos
            if (token.IsCancellationRequested)
                return;

            // Actualizar la propiedad solo si la tarea sigue siendo válida
                if (result.IsSuccess)
                {
                    PreviewContent = result.Result;
                    StatusMessage = "Preview actualizado correctamente.";
                }
                else
                {
                    PreviewContent = string.Empty; // O mantener el anterior?
                    StatusMessage = $"Error: {result.ErrorMessage}";
                }
        }
        catch (TaskCanceledException)
        {
            // La tarea fue cancelada porque el usuario escribió algo nuevo. Ignoramos.
        }
    }
}
