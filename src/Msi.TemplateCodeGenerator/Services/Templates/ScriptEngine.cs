using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Scriban;
using Scriban.Runtime;

namespace Msi.TemplateCodeGenerator.Services.Templates;

/// <summary>
/// Motor de ejecución de scripts Scriban.
/// Único punto de entrada para cualquier operación con Scriban en la aplicación.
/// Registra helpers C# y write_to_file en el TemplateContext.
/// </summary>
internal sealed class ScriptEngine(
    IScriptOutputWriter outputWriter,
    IElementCatalog elementCatalog,
    ILogger<ScriptEngine> logger) : IScriptEngine
{
    private readonly IScriptOutputWriter _outputWriter = outputWriter;
    private readonly IElementCatalog _elementCatalog = elementCatalog;
    private readonly ILogger<ScriptEngine> _logger = logger;

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> ExecuteAsync(string scriptContent, string scriptPath, bool preview = false)
    {
        bool success;
        List<string> errors = new();
        List<ScriptOutput> outputs = new();
        string renderedContent = string.Empty;

        try
        {
            Template template = Template.Parse(scriptContent, scriptPath);

            if (template.HasErrors)
            {
                errors.AddRange(template.Messages.Select(m => m.Message));
                return new ScriptExecutionResult
                {
                    Success = false,
                    Errors = errors,
                    Outputs = outputs,
                    RenderedContent = renderedContent
                };
            }

            List<ScriptOutput> collectedOutputs = new();
            TemplateContext context = BuildContext(collectedOutputs);

            renderedContent = await template.RenderAsync(context);

            if (!preview)
            {
                foreach (ScriptOutput output in collectedOutputs)
                {
                    await _outputWriter.WriteToFile(output.Path, output.Content);
                }
            }

            outputs = collectedOutputs;
            success = true;

            _logger.LogInformation("Script {Path} executed successfully with {Count} outputs",
                scriptPath, outputs.Count);
        }
        catch (Exception ex)
        {
            success = false;
            errors.Add(ex.Message);
            _logger.LogError(ex, "Error executing script {Path}", scriptPath);
        }

        return new ScriptExecutionResult
        {
            Success = success,
            Errors = errors,
            Outputs = outputs,
            RenderedContent = renderedContent
        };
    }

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> ProcessPreviewAsync(string templateContent)
    {
        List<string> errors = new();
        string renderedContent = string.Empty;
        bool success;

        try
        {
            Template template = Template.Parse(templateContent);

            if (template.HasErrors)
            {
                errors.AddRange(template.Messages.Select(m => m.Message));
                return new ScriptExecutionResult
                {
                    Success = false,
                    Errors = errors,
                    Outputs = [],
                    RenderedContent = string.Empty
                };
            }

            TemplateContext context = BuildContext(null);
            renderedContent = await template.RenderAsync(context);
            success = true;
        }
        catch (Exception ex)
        {
            success = false;
            errors.Add(ex.Message);
            _logger.LogError(ex, "Error processing preview");
        }

        return new ScriptExecutionResult
        {
            Success = success,
            Errors = errors,
            Outputs = [],
            RenderedContent = renderedContent
        };
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ValidateSyntaxAsync(string templateContent)
    {
        Template template = Template.Parse(templateContent);

        IReadOnlyList<string> errors = template.HasErrors
            ? template.Messages.Select(m => m.Message).ToList()
            : (IReadOnlyList<string>)Array.Empty<string>();

        return Task.FromResult(errors);
    }

    private TemplateContext BuildContext(List<ScriptOutput>? collectedOutputs)
    {
        TemplateContext context = new();
        context.MemberRenamer = member => member.Name;
        context.MemberFilter = member => true;

        ScriptHelpers helpers = new(_elementCatalog);
        ScriptObject functions = new();
        functions.Import("GetAllElements", new Func<IEnumerable<Element>>(helpers.GetAllElements));
        functions.Import("GetElementsByType", new Func<string, IEnumerable<Element>>(helpers.GetElementsByType));
        functions.Import("PascalCase", new Func<string, string>(ScriptHelpers.PascalCase));
        functions.Import("CamelCase", new Func<string, string>(ScriptHelpers.CamelCase));
        context.PushGlobal(functions);

        // Registrar write_to_file: si collectedOutputs es null (preview), es no-op
        ScriptObject writeFunctions = new();
        if (collectedOutputs is not null)
        {
            writeFunctions.Import("write_to_file", new Action<string, string>((path, content) =>
            {
                collectedOutputs.Add(new ScriptOutput { Path = path, Content = content });
            }));
        }
        else
        {
            // No-op durante preview
            writeFunctions.Import("write_to_file", new Action<string, string>((path, content) => { }));
        }
        context.PushGlobal(writeFunctions);

        return context;
    }
}
