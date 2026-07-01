namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa un elemento del proyecto (ej: una clase, entidad, etc.)
/// </summary>
internal sealed class DummyElement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BaseClass { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<DummyField> Fields { get; set; } = new();
}
