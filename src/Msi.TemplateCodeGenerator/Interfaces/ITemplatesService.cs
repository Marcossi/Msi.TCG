using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Services.Templates;

namespace Msi.TemplateCodeGenerator.Interfaces;

public interface ITemplatesService
{
    Task<TemplateResult> ProcessTemplateAsync(string template);

    /// <summary>
    /// Ejecuta un script .scriban contra el catálogo de Elements.
    /// </summary>
    /// <param name="scriptPath">Ruta absoluta del fichero .scriban.</param>
    /// <returns>Resultado de la ejecución.</returns>
    Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath);

    /// <summary>
    /// Ejecuta todos los scripts del proyecto.
    /// </summary>
    /// <returns>Resultado agregado con conteo de éxitos y errores.</returns>
    Task<BatchExecutionResult> ExecuteAllScriptsAsync();
}
