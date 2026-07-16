using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

/// <summary>
/// ViewModel del editor de plantillas Scriban.
/// Hereda el manejo de fichero y contenido de BaseTextEditorViewModel
/// y añade el renderizado en tiempo real con debounce usando IScriptEngine.
/// </summary>
internal partial class TemplateEditorShellViewModel(
    IScriptEngine scriptEngine,
    IFileSystem fileSystem,
    IDialogService dialogService,
    ILogger<TemplateEditorShellViewModel> logger)
    : BaseTextEditorViewModel(fileSystem, dialogService, logger)
{
    private readonly IScriptEngine _scriptEngine = scriptEngine;

    [ObservableProperty]
    private string _previewContent = string.Empty;

    [ObservableProperty]
    private string _previewError = string.Empty;

    private CancellationTokenSource? _debounceCts;

    /// <inheritdoc/>
    protected override void OnContentChangedCore(string value)
    {
        _logger.LogDebug("Contenido cambiado ({CharCount} chars), reiniciando debounce", value.Length);

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        _ = UpdatePreviewWithDebounceAsync(value, token);
    }

    private async Task UpdatePreviewWithDebounceAsync(string content, CancellationToken token)
    {
        try
        {
            await Task.Delay(1000, token);

            if (token.IsCancellationRequested)
            {
                _logger.LogDebug("Debounce cancelado por nuevo cambio de contenido");
                return;
            }

            _logger.LogDebug("Renderizando preview con debounce");
            ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(content, FilePath, preview: true);

            if (token.IsCancellationRequested)
                return;

            if (result.Success)
            {
                PreviewContent = result.RenderedContent;
                PreviewError = string.Empty;

                StatusMessage = "Preview actualizado correctamente.";
                _logger.LogDebug("Preview actualizado exitosamente");
            }
            else
            {
                PreviewContent = string.Empty;
                PreviewError = string.Join("\n", result.Errors);
                StatusMessage = $"Error: {PreviewError}";
                _logger.LogWarning("Error en preview: {Errors}", PreviewError);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    /// <summary>
    /// Ejecuta el script actual y escribe los outputs a disco.
    /// </summary>
    [RelayCommand]
    private async Task GenerateAsync()
    {
        _logger.LogInformation("[UI] Command: Generate");
        try
        {
            ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(Content, FilePath, preview: false);

            if (result.Success)
            {
                _logger.LogInformation("Script {Path} generated {Count} files",
                    FilePath, result.Outputs.Count);

                StatusMessage = $"Generated {result.Outputs.Count} file(s)";
            }
            else
            {
                string errors = string.Join("\n", result.Errors);
                StatusMessage = $"Error: {errors}";
                _logger.LogWarning("Script {Path} failed: {Errors}", FilePath, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating from script {Path}", FilePath);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public override bool CanExecute(string commandName) => commandName switch
    {
        "Generate" => !string.IsNullOrEmpty(FilePath),
        _ => base.CanExecute(commandName)
    };

    public override async Task ExecuteAsync(string commandName)
    {
        if (commandName == "Generate")
        {
            await GenerateAsync();
            return;
        }

        await base.ExecuteAsync(commandName);
    }
}
