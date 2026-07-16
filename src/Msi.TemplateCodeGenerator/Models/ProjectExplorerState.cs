namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Estado persistente de la UI del explorador de proyectos.
/// </summary>
/// <param name="ExpandedPaths">Rutas relativas de las carpetas expandidas.</param>
/// <param name="ActiveDocumentRelativePath">Ruta relativa del documento activo, si existe.</param>
public sealed record ProjectExplorerState(
    IReadOnlyList<string> ExpandedPaths,
    string? ActiveDocumentRelativePath
);
