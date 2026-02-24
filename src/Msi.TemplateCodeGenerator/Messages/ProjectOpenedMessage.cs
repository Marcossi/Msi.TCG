namespace Msi.TemplateCodeGenerator.Messages;

/// <summary>
/// Mensaje enviado cuando se abre un proyecto.
/// </summary>
/// <param name="ProjectPath">Ruta del proyecto abierto.</param>
public sealed record ProjectOpenedMessage(string ProjectPath);
