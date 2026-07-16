using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services.Templates;

/// <summary>
/// Helpers C# disponibles para scripts Scriban.
/// Se registran en el TemplateContext para acceso desde scripts.
/// Los métodos que acceden al catálogo son de instancia (DI por constructor).
/// Los métodos de utilidad pura son estáticos.
/// </summary>
public sealed class ScriptHelpers
{
    private readonly IElementCatalog _catalog;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ScriptHelpers"/>.
    /// </summary>
    /// <param name="catalog">Catálogo de Elements para acceso desde scripts.</param>
    public ScriptHelpers(IElementCatalog catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// Obtiene todos los Elements del catálogo.
    /// </summary>
    /// <returns>Colección de todos los Elements.</returns>
    public IEnumerable<Element> GetAllElements()
    {
        return _catalog.GetAll();
    }

    /// <summary>
    /// Obtiene todos los Elements de un tipo específico.
    /// </summary>
    /// <param name="type">Tipo del Element (ej: "Workflow").</param>
    /// <returns>Elements del tipo especificado.</returns>
    public IEnumerable<Element> GetElementsByType(string type)
    {
        return _catalog.GetByType(type);
    }

    /// <summary>
    /// Convierte un string a PascalCase.
    /// </summary>
    /// <param name="input">String de entrada.</param>
    /// <returns>String en PascalCase.</returns>
    public static string PascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        if (input.Contains('_') || input.Contains(' ') || input.Contains('-'))
        {
            return string.Concat(
                input.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant())
            );
        }

        if (char.IsLower(input[0]))
        {
            return char.ToUpperInvariant(input[0]) + input.Substring(1);
        }

        return input;
    }

    /// <summary>
    /// Convierte un string a camelCase.
    /// </summary>
    /// <param name="input">String de entrada.</param>
    /// <returns>String en camelCase.</returns>
    public static string CamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string pascalCase = PascalCase(input);
        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }
}
