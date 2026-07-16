using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Motor de ejecución de scripts Scriban.
/// Único punto de entrada para cualquier operación con Scriban.
/// </summary>
public interface IScriptEngine
{
    /// <summary>
    /// Ejecuta un script Scriban.
    /// </summary>
    /// <param name="scriptContent">Contenido del script.</param>
    /// <param name="scriptPath">Ruta del script (para mensajes de error).</param>
    /// <param name="preview">Si es true, no escribe a disco (solo captura en memoria).</param>
    /// <returns>Resultado de la ejecución.</returns>
    Task<ScriptExecutionResult> ExecuteAsync(string scriptContent, string scriptPath, bool preview = false);

    /// <summary>
    /// Procesa una plantilla para vista previa en tiempo real.
    /// Construye el modelo desde IElementCatalog y renderiza la plantilla.
    /// </summary>
    /// <param name="templateContent">Contenido de la plantilla.</param>
    /// <returns>Resultado con el contenido renderizado o errores.</returns>
    Task<ScriptExecutionResult> ProcessPreviewAsync(string templateContent);

    /// <summary>
    /// Valida la sintaxis de una plantilla Scriban sin ejecutarla.
    /// </summary>
    /// <param name="templateContent">Contenido de la plantilla.</param>
    /// <returns>Lista de errores de sintaxis (vacía si la sintaxis es válida).</returns>
    Task<IReadOnlyList<string>> ValidateSyntaxAsync(string templateContent);
}
