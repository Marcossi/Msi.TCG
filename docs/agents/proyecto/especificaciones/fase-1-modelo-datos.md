# Especificación técnica: Fase 1 - Modelo de datos y carga de JSONs

> Implementación del modelo de datos (Element + ElementProperty) y carga de JSONs desde el proyecto.
> Referencia: ADR-005, Fase 1

## Objetivo

Tener Elements cargados desde JSON en memoria, con tolerancia a errores.

## Alcance

### Incluye
- Clases `Element` y `ElementProperty` con método `Get<T>()`
- Interfaz `IElementCatalog` + implementación
- Carga de JSONs desde carpeta del proyecto
- Tolerancia a errores: JSON inválido → log + ignorar

### No incluye
- Scripting Scriban
- Helpers C#
- UI
- write_to_file
- Preview
- FileWatcher

## Modelo de datos

### Clase Element

```csharp
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
```

### Clase ElementProperty

```csharp
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
    
    /// <summary>
    /// Indica si la propiedad es obligatoria.
    /// </summary>
    public bool IsRequired { get; set; }
}
```

## Formato JSON

### Estructura básica

```json
{
  "Id": "wf-001",
  "Name": "OrderProcessing",
  "Type": "Workflow",
  "Properties": [
    {
      "Name": "Namespace",
      "Type": "string",
      "Value": "MyApp.Workflows",
      "IsRequired": true
    },
    {
      "Name": "Description",
      "Type": "string",
      "Value": "Procesa órdenes de clientes",
      "IsRequired": false
    },
    {
      "Name": "MaxRetries",
      "Type": "int",
      "Value": 3,
      "IsRequired": false
    },
    {
      "Name": "IsActive",
      "Type": "bool",
      "Value": true,
      "IsRequired": false
    }
  ]
}
```

### Tipos soportados

| Type | Valor en JSON | Ejemplo |
|------|---------------|---------|
| `string` | String | `"Value": "texto"` |
| `int` | Número entero | `"Value": 42` |
| `bool` | Booleano | `"Value": true` |
| `double` | Número decimal | `"Value": 3.14` |
| `array` | Array JSON | `"Value": [1, 2, 3]` |
| `object` | Objeto JSON | `"Value": {"key": "value"}` |

**Nota:** Los tipos `array` y `object` se deserializan como `List<object>` y `Dictionary<string, object>` respectivamente.

### Validaciones al cargar

1. **Campos obligatorios:**
   - `Id` no puede estar vacío
   - `Name` no puede estar vacío
   - `Type` no puede estar vacío

2. **Propiedades:**
   - Cada propiedad debe tener `Name` y `Type`
   - `Value` puede ser null (opcional)

3. **Consistencia:**
   - `Id` debe ser único dentro del proyecto (warning si hay duplicados)

## Contratos de servicios

### IElementCatalog

```csharp
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
```

### LoadError

```csharp
namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa un error al cargar un fichero JSON.
/// </summary>
public sealed class LoadError
{
    /// <summary>
    /// Ruta del fichero que causó el error.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Mensaje de error.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Excepción original (si aplica).
    /// </summary>
    public Exception? Exception { get; set; }
}
```

### ElementCatalog (implementación)

```csharp
namespace Msi.TemplateCodeGenerator.Services;

internal sealed class ElementCatalog : IElementCatalog
{
    private readonly IProjectContext _projectContext;
    private readonly IFileService _fileService;
    private readonly ILogger<ElementCatalog> _logger;
    private List<Element> _elements = new();
    private List<LoadError> _loadErrors = new();
    
    public ElementCatalog(
        IProjectContext projectContext,
        IFileService fileService,
        ILogger<ElementCatalog> logger)
    {
        _projectContext = projectContext;
        _fileService = fileService;
        _logger = logger;
    }
    
    public IEnumerable<Element> GetAll() => _elements;
    
    public Element? GetById(string id) => _elements.FirstOrDefault(e => e.Id == id);
    
    public IEnumerable<Element> GetByType(string type) => _elements.Where(e => e.Type == type);
    
    public async Task ReloadAsync()
    {
        _elements.Clear();
        _loadErrors.Clear();
        
        string projectPath = _projectContext.CurrentProject?.FolderPath 
            ?? throw new InvalidOperationException("No project is currently open");
        
        // Buscar todos los ficheros .json en el proyecto
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(projectPath, "*.json", SearchOption.AllDirectories);
        
        foreach (string jsonFile in jsonFiles)
        {
            try
            {
                string content = await _fileService.ReadTextAsync(jsonFile);
                Element? element = DeserializeElement(content);
                
                if (element != null)
                {
                    // Validar campos obligatorios
                    if (string.IsNullOrEmpty(element.Id) || 
                        string.IsNullOrEmpty(element.Name) || 
                        string.IsNullOrEmpty(element.Type))
                    {
                        _loadErrors.Add(new LoadError
                        {
                            FilePath = jsonFile,
                            Message = "Element missing required fields (Id, Name, or Type)"
                        });
                        _logger.LogWarning("Element in {File} missing required fields", jsonFile);
                        continue;
                    }
                    
                    // Validar Id único
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
    }
    
    public IReadOnlyList<LoadError> GetLoadErrors() => _loadErrors;
    
    private Element? DeserializeElement(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        
        Element element = new()
        {
            Id = root.GetProperty("Id").GetString() ?? string.Empty,
            Name = root.GetProperty("Name").GetString() ?? string.Empty,
            Type = root.GetProperty("Type").GetString() ?? string.Empty
        };
        
        if (root.TryGetProperty("Properties", out JsonElement propertiesElement))
        {
            foreach (JsonElement propElement in propertiesElement.EnumerateArray())
            {
                ElementProperty property = new()
                {
                    Name = propElement.GetProperty("Name").GetString() ?? string.Empty,
                    Type = propElement.GetProperty("Type").GetString() ?? string.Empty,
                    IsRequired = propElement.TryGetProperty("IsRequired", out JsonElement isRequiredElement) 
                        && isRequiredElement.GetBoolean()
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
            JsonValueKind.Number => element.TryGetInt32(out int intVal) 
                ? intVal 
                : element.GetDouble(),
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
```

## Registro en DI

```csharp
// En DependencyInjection.cs
services.AddSingleton<IElementCatalog, ElementCatalog>();
```

## Integración con ProjectService

```csharp
// En ProjectService.OpenProjectAsync
public async Task OpenProjectAsync(string projectPath)
{
    // ... código existente ...
    
    // Recargar catálogo de Elements
    await _elementCatalog.ReloadAsync();
    
    // ... resto del código ...
}
```

## Testing

### Unit tests

```csharp
public class ElementTests
{
    [Fact]
    public void Get_WithExistingProperty_ReturnsValue()
    {
        // Arrange
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>
            {
                new() { Name = "Namespace", Type = "string", Value = "MyApp" }
            }
        };
        
        // Act
        string result = element.Get<string>("Namespace");
        
        // Assert
        result.ShouldBe("MyApp");
    }
    
    [Fact]
    public void Get_WithNonExistentProperty_ThrowsException()
    {
        // Arrange
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>()
        };
        
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => element.Get<string>("NonExistent"));
    }
    
    [Fact]
    public void Get_WithWrongType_ThrowsException()
    {
        // Arrange
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>
            {
                new() { Name = "Count", Type = "int", Value = 42 }
            }
        };
        
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => element.Get<string>("Count"));
    }
    
    [Fact]
    public void TryGet_WithExistingProperty_ReturnsTrueAndValue()
    {
        // Arrange
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>
            {
                new() { Name = "IsActive", Type = "bool", Value = true }
            }
        };
        
        // Act
        bool success = element.TryGet<bool>("IsActive", out bool? value);
        
        // Assert
        success.ShouldBeTrue();
        value.ShouldBeTrue();
    }
    
    [Fact]
    public void TryGet_WithNonExistentProperty_ReturnsFalse()
    {
        // Arrange
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>()
        };
        
        // Act
        bool success = element.TryGet<string>("NonExistent", out string? value);
        
        // Assert
        success.ShouldBeFalse();
        value.ShouldBeNull();
    }
}
```

### Integration tests

```csharp
public class ElementCatalogTests
{
    [Fact]
    public async Task ReloadAsync_WithValidJson_LoadsElements()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        string json = """
        {
          "Id": "wf-001",
          "Name": "OrderProcessing",
          "Type": "Workflow",
          "Properties": [
            { "Name": "Namespace", "Type": "string", "Value": "MyApp.Workflows" }
          ]
        }
        """;
        
        await File.WriteAllTextAsync(Path.Combine(tempDir, "workflow.json"), json);
        
        var projectContext = new ProjectContext { CurrentProject = new Project { FolderPath = tempDir } };
        var fileService = new FileService();
        var logger = new Logger<ElementCatalog>(new LoggerFactory());
        var catalog = new ElementCatalog(projectContext, fileService, logger);
        
        // Act
        await catalog.ReloadAsync();
        
        // Assert
        catalog.GetAll().ShouldHaveSingleItem();
        catalog.GetById("wf-001").ShouldNotBeNull();
        catalog.GetById("wf-001")!.Name.ShouldBe("OrderProcessing");
        
        // Cleanup
        Directory.Delete(tempDir, recursive: true);
    }
    
    [Fact]
    public async Task ReloadAsync_WithInvalidJson_LogsErrorAndContinues()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        await File.WriteAllTextAsync(Path.Combine(tempDir, "invalid.json"), "{ invalid json }");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "valid.json"), """
        {
          "Id": "test-1",
          "Name": "Test",
          "Type": "Test",
          "Properties": []
        }
        """);
        
        var projectContext = new ProjectContext { CurrentProject = new Project { FolderPath = tempDir } };
        var fileService = new FileService();
        var logger = new Logger<ElementCatalog>(new LoggerFactory());
        var catalog = new ElementCatalog(projectContext, fileService, logger);
        
        // Act
        await catalog.ReloadAsync();
        
        // Assert
        catalog.GetAll().ShouldHaveSingleItem();
        catalog.GetLoadErrors().ShouldHaveSingleItem();
        catalog.GetLoadErrors()[0].FilePath.ShouldEndWith("invalid.json");
        
        // Cleanup
        Directory.Delete(tempDir, recursive: true);
    }
}
```

## Criterios de aceptación

- [ ] `dotnet build` sin errores
- [ ] Test unitario: crear JSON válido → se carga como Element correcto
- [ ] Test unitario: JSON inválido → se ignora, no crashea
- [ ] Test unitario: `Get<T>()` con propiedad existente → retorna valor
- [ ] Test unitario: `Get<T>()` con propiedad inexistente → lanza excepción
- [ ] Test unitario: `TryGet<T>()` funciona correctamente
- [ ] Log muestra qué JSONs se cargaron y cuáles fallaron
- [ ] `IElementCatalog` registrado en DI y accesible desde servicios

## Ejemplo de uso

```csharp
// En un servicio o ViewModel
public class SomeService
{
    private readonly IElementCatalog _catalog;
    
    public SomeService(IElementCatalog catalog)
    {
        _catalog = catalog;
    }
    
    public void ProcessWorkflows()
    {
        IEnumerable<Element> workflows = _catalog.GetByType("Workflow");
        
        foreach (Element workflow in workflows)
        {
            string name = workflow.Get<string>("Name");
            string namespace = workflow.Get<string>("Namespace");
            
            Console.WriteLine($"Processing workflow: {name} in {namespace}");
        }
    }
}
```

## Pendiente de definir

- Detalles de encoding y line endings en JSONs
- Soporte para comentarios en JSON (JSONC)
- Validación de schemas por tipo de Element
