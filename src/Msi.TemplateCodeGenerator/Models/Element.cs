namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa una entidad de dominio cargada desde JSON.
/// Modelo universal: todas las entidades (Workflow, Vista, etc.) se representan con esta clase.
/// </summary>
public sealed class Element
{
    /// <summary>
    /// Identificador único del elemento.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre legible del elemento.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tipo del elemento (ej: "Workflow", "Vista", "WorkflowId").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Propiedades dinámicas del elemento.
    /// </summary>
    public List<ElementProperty> Properties { get; set; } = new();

    /// <summary>
    /// Obtiene el valor de una propiedad por nombre, con validación de tipo.
    /// </summary>
    /// <typeparam name="T">Tipo esperado del valor.</typeparam>
    /// <param name="propertyName">Nombre de la propiedad.</param>
    /// <returns>Valor de la propiedad convertido al tipo T.</returns>
    /// <exception cref="InvalidOperationException">Si la propiedad no existe o el tipo no coincide.</exception>
    public T Get<T>(string propertyName)
    {
        ElementProperty? property = Properties.FirstOrDefault(p => p.Name == propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found in element '{Name}' (Type: {Type}, Id: {Id})");
        }

        if (property.Value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' in element '{Name}' has type '{property.Type}' but expected '{typeof(T).Name}'");
    }

    /// <summary>
    /// Intenta obtener el valor de una propiedad por nombre.
    /// </summary>
    /// <typeparam name="T">Tipo esperado del valor.</typeparam>
    /// <param name="propertyName">Nombre de la propiedad.</param>
    /// <param name="value">Valor de la propiedad si existe y tiene el tipo correcto.</param>
    /// <returns>True si la propiedad existe y tiene el tipo correcto; false en caso contrario.</returns>
    public bool TryGet<T>(string propertyName, out T? value)
    {
        ElementProperty? property = Properties.FirstOrDefault(p => p.Name == propertyName);

        if (property != null && property.Value is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }
}
