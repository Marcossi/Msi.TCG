using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services.Templates;

internal sealed class TemplatesService(
    IScriptEngine scriptEngine,
    IFileSystem fileSystem,
    IProjectContext projectContext,
    IMessenger messenger,
    ILogger<TemplatesService> logger) : ITemplatesService
{
    private readonly IScriptEngine _scriptEngine = scriptEngine;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IProjectContext _projectContext = projectContext;
    private readonly IMessenger _messenger = messenger;
    private readonly ILogger<TemplatesService> _logger = logger;

    /// <inheritdoc/>
    public async Task<TemplateResult> ProcessTemplateAsync(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            _logger.LogDebug("ProcessTemplateAsync: contenido vacio, retornando exito");
            return TemplateResult.Success(string.Empty);
        }

        _logger.LogDebug("Procesando plantilla ({CharCount} chars)", templateContent.Length);

        ScriptExecutionResult result = await _scriptEngine.ProcessPreviewAsync(templateContent);

        if (result.Success)
        {
            _logger.LogDebug("Plantilla renderizada exitosamente ({ResultLen} chars)", result.RenderedContent.Length);
            return TemplateResult.Success(result.RenderedContent);
        }

        string errorMessage = string.Join("\n", result.Errors);
        _logger.LogWarning("Error al procesar plantilla: {Errors}", errorMessage);
        return TemplateResult.Failure(errorMessage);
    }

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath)
    {
        string scriptContent = await _fileSystem.ReadTextAsync(scriptPath);
        ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(scriptContent, scriptPath, preview: false);

        _messenger.Send(new ScriptExecutionCompletedMessage(
            scriptPath,
            result.Success,
            result.Errors));

        return result;
    }

    /// <inheritdoc/>
    public async Task<BatchExecutionResult> ExecuteAllScriptsAsync()
    {
        IEnumerable<string> scriptPaths = await GetScriptPaths();

        int successCount = 0;
        int errorCount = 0;
        List<string> errors = new();

        foreach (string scriptPath in scriptPaths)
        {
            ScriptExecutionResult result = await ExecuteScriptAsync(scriptPath);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                errorCount++;
                errors.Add($"{Path.GetFileName(scriptPath)}: {string.Join(", ", result.Errors)}");
            }
        }

        return new BatchExecutionResult
        {
            SuccessCount = successCount,
            ErrorCount = errorCount,
            Errors = errors
        };
    }

    private async Task<IEnumerable<string>> GetScriptPaths()
    {
        string projectPath = _projectContext.CurrentProject?.FolderPath ?? string.Empty;

        if (string.IsNullOrEmpty(projectPath) || !await _fileSystem.DirectoryExistsAsync(projectPath))
            return Enumerable.Empty<string>();

        IReadOnlyList<string> files = await _fileSystem.EnumerateFilesAsync(projectPath, "*.scriban", SearchOption.AllDirectories);
        return files;
    }
}
