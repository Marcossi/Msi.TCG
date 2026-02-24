namespace Msi.TemplateCodeGenerator.Messages;

/// <summary>
/// Mensaje enviado cuando se guarda un proyecto.
/// </summary>
/// <param name="ProjectPath">Ruta del proyecto guardado.</param>
public sealed record ProjectSavedMessage(string ProjectPath);
