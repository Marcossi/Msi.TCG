namespace Msi.TemplateCodeGenerator.Messages;

/// <summary>
/// Mensaje enviado cuando un script se ejecuta (individual o batch).
/// </summary>
/// <param name="ScriptPath">Ruta del script ejecutado.</param>
/// <param name="Success">Indica si la ejecución fue exitosa.</param>
/// <param name="Errors">Mensajes de error (vacío si Success es true).</param>
public sealed record ScriptExecutionCompletedMessage(string ScriptPath, bool Success, IReadOnlyList<string> Errors);
