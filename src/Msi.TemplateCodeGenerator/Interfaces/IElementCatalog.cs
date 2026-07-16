using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Catálogo de todos los Elements cargados desde JSON.
/// </summary>
public interface IElementCatalog
{
    /// <summary>
    /// Obtiene todos los Elements del catálogo.
    /// </summary>
    IEnumerable<Element> GetAll();

    /// <summary>
    /// Obtiene un Element por su Id.
    /// </summary>
    /// <param name="id">Identificador del Element.</param>
    /// <returns>El Element si existe; null en caso contrario.</returns>
    Element? GetById(string id);

    /// <summary>
    /// Obtiene todos los Elements de un tipo específico.
    /// </summary>
    /// <param name="type">Tipo del Element (ej: "Workflow").</param>
    /// <returns>Elements del tipo especificado.</returns>
    IEnumerable<Element> GetByType(string type);

    /// <summary>
    /// Recarga todos los Elements desde disco.
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Obtiene los errores de carga de JSONs.
    /// </summary>
    IReadOnlyList<LoadError> GetLoadErrors();
}
