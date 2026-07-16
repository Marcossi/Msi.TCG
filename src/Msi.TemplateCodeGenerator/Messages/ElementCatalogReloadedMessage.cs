using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Messages;

/// <summary>
/// Mensaje enviado cuando el catálogo de Elements se recarga desde disco.
/// </summary>
/// <param name="ElementCount">Número de Elements cargados.</param>
/// <param name="Errors">Errores de carga detectados.</param>
public sealed record ElementCatalogReloadedMessage(int ElementCount, IReadOnlyList<LoadError> Errors);
