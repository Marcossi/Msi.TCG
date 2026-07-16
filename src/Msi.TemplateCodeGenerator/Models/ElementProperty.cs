namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa una propiedad dinámica de un Element.
/// Similar a PropertyInfo en reflection: tiene nombre, tipo y valor.
/// </summary>
public sealed class ElementProperty
{
    /// <summary>
    /// Nombre de la propiedad (ej: "Namespace", "Description").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dato de la propiedad (ej: "string", "int", "bool", "Activity").
    /// Permite switch por tipo en scripts Scriban.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Valor de la propiedad. Puede ser string, int, bool, List, Dictionary, etc.
    /// </summary>
    public object? Value { get; set; }
}
