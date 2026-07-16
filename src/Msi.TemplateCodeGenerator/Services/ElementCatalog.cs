using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IElementCatalog"/> que carga Elements desde ficheros JSON en disco.
/// Tolerante a errores: JSON inválido se loguea y se ignora.
/// Publica <see cref="ElementCatalogReloadedMessage"/> tras cada recarga.
/// </summary>
internal sealed class ElementCatalog(
    IProjectContext projectContext,
    IFileSystem fileSystem,
    IMessenger messenger,
    ILogger<ElementCatalog> logger) : IElementCatalog
{
    private readonly IProjectContext _projectContext = projectContext;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IMessenger _messenger = messenger;
    private readonly ILogger<ElementCatalog> _logger = logger;
    private List<Element> _elements = new();
    private List<LoadError> _loadErrors = new();

    /// <inheritdoc/>
    public IEnumerable<Element> GetAll() => _elements;

    /// <inheritdoc/>
    public Element? GetById(string id) => _elements.FirstOrDefault(e => e.Id == id);

    /// <inheritdoc/>
    public IEnumerable<Element> GetByType(string type) => _elements.Where(e => e.Type == type);

    /// <inheritdoc/>
    public async Task ReloadAsync()
    {
        _elements.Clear();
        _loadErrors.Clear();

        string projectPath = _projectContext.CurrentProject?.FolderPath
            ?? throw new InvalidOperationException("No project is currently open");

        IReadOnlyList<string> jsonFiles = await _fileSystem.EnumerateFilesAsync(projectPath, "*.json", SearchOption.AllDirectories);

        foreach (string jsonFile in jsonFiles)
        {
            try
            {
                string content = await _fileSystem.ReadTextAsync(jsonFile);
                Element? element = DeserializeElement(content);

                if (element != null)
                {
                    if (string.IsNullOrEmpty(element.Id)
                        || string.IsNullOrEmpty(element.Name)
                        || string.IsNullOrEmpty(element.Type))
                    {
                        _loadErrors.Add(new LoadError
                        {
                            FilePath = jsonFile,
                            Message = "Element missing required fields (Id, Name, or Type)"
                        });
                        _logger.LogWarning("Element in {File} missing required fields", jsonFile);
                        continue;
                    }

                    if (_elements.Any(e => e.Id == element.Id))
                    {
                        _loadErrors.Add(new LoadError
                        {
                            FilePath = jsonFile,
                            Message = $"Duplicate Element Id: {element.Id}"
                        });
                        _logger.LogWarning("Duplicate Element Id {Id} in {File}", element.Id, jsonFile);
                        continue;
                    }

                    _elements.Add(element);
                }
            }
            catch (Exception ex)
            {
                _loadErrors.Add(new LoadError
                {
                    FilePath = jsonFile,
                    Message = ex.Message,
                    Exception = ex
                });
                _logger.LogError(ex, "Error loading Element from {File}", jsonFile);
            }
        }

        _logger.LogInformation("Loaded {Count} Elements with {ErrorCount} errors", _elements.Count, _loadErrors.Count);

        _messenger.Send(new ElementCatalogReloadedMessage(_elements.Count, _loadErrors));
    }

    /// <inheritdoc/>
    public IReadOnlyList<LoadError> GetLoadErrors() => _loadErrors;

    private Element? DeserializeElement(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        // Buscar en la raíz o dentro de "data"
        JsonElement dataElement = root;
        if (root.TryGetProperty("data", out JsonElement nestedData))
        {
            dataElement = nestedData;
        }

        if (!dataElement.TryGetProperty("Id", out JsonElement idElement) ||
            !dataElement.TryGetProperty("Name", out JsonElement nameElement) ||
            !dataElement.TryGetProperty("Type", out JsonElement typeElement))
        {
            return null;
        }

        Element element = new()
        {
            Id = idElement.GetString() ?? string.Empty,
            Name = nameElement.GetString() ?? string.Empty,
            Type = typeElement.GetString() ?? string.Empty
        };

        if (dataElement.TryGetProperty("Properties", out JsonElement propertiesElement))
        {
            foreach (JsonElement propElement in propertiesElement.EnumerateArray())
            {
                ElementProperty property = new()
                {
                    Name = propElement.GetProperty("Name").GetString() ?? string.Empty,
                    Type = propElement.GetProperty("Type").GetString() ?? string.Empty
                };

                if (propElement.TryGetProperty("Value", out JsonElement valueElement))
                {
                    property.Value = DeserializeValue(valueElement);
                }

                element.Properties.Add(property);
            }
        }

        return element;
    }

    private object? DeserializeValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => int.TryParse(element.GetRawText(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal)
                ? intVal
                : double.TryParse(element.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleVal)
                    ? doubleVal
                    : null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => DeserializeArray(element),
            JsonValueKind.Object => DeserializeObject(element),
            _ => null
        };
    }

    private List<object?> DeserializeArray(JsonElement element)
    {
        List<object?> list = new();
        foreach (JsonElement item in element.EnumerateArray())
        {
            list.Add(DeserializeValue(item));
        }
        return list;
    }

    private Dictionary<string, object?> DeserializeObject(JsonElement element)
    {
        Dictionary<string, object?> dict = new();
        foreach (JsonProperty prop in element.EnumerateObject())
        {
            dict[prop.Name] = DeserializeValue(prop.Value);
        }
        return dict;
    }
}
