namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa un campo de un elemento del proyecto.
/// </summary>
internal sealed class DummyField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Accessibility { get; set; } = "private";
}
