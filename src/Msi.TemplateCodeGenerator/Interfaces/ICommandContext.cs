namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contexto de comandos. Expone el documento o tool activo que puede manejar comandos contextuales.
/// Se integra con INavigationService para trackear el foco en el layout de Dock.Avalonia.
/// </summary>
public interface ICommandContext
{
    /// <summary>
    /// Obtiene la ruta de comandos activa (el VM del documento/tool con foco).
    /// Devuelve null si no hay documento activo o el activo no implementa ICommandRoute.
    /// </summary>
    ICommandRoute? ActiveRoute { get; }
}
