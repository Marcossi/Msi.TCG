# Especificación técnica: Fase 2 - Motor de scripts y helpers C#

> Implementación del motor de ejecución de scripts Scriban con helpers C# y write_to_file.
> Referencia: ADR-005, Fase 2

## Objetivo

Un script puede generar ficheros usando Elements y write_to_file.

## Alcance

### Incluye
- `ScriptHelpers` con `GetAllElements()` y `PascalCase()`
- Función `write_to_file` registrada en TemplateContext
- Motor que ejecuta un script .scriban contra el catálogo
- Integración con `ITemplatesService`

### No incluye
- Cambios en UI
- Preview
- Tolerancia a errores en scripts
- FileWatcher
- Combo de múltiples outputs

### Dependencias
- **Fase 1 completada**: `IElementCatalog` debe estar disponible

## ScriptHelpers

### Clase ScriptHelpers

```csharp
namespace Msi.TemplateCodeGenerator.Services.Templates;

/// <summary>
/// Helpers C# disponibles para scripts Scriban.
/// Se registran en el TemplateContext para acceso desde scripts.
/// </summary>
public static class ScriptHelpers
{
    /// <summary>
    /// Obtiene todos los Elements del catálogo.
    /// </summary>
    /// <returns>Colección de todos los Elements.</returns>
    public static IEnumerable<Element> GetAllElements()
    {
        IElementCatalog catalog = ServiceProvider.GetRequiredService<IElementCatalog>();
        return catalog.GetAll();
    }
    
    /// <summary>
    /// Obtiene todos los Elements de un tipo específico.
    /// </summary>
    /// <param name="type">Tipo del Element (ej: "Workflow").</param>
    /// <returns>Elements del tipo especificado.</returns>
    public static IEnumerable<Element> GetElementsByType(string type)
    {
        IElementCatalog catalog = ServiceProvider.GetRequiredService<IElementCatalog>();
        return catalog.GetByType(type);
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
        
        // Si ya tiene guiones bajos o espacios, convertir
        if (input.Contains('_') || input.Contains(' ') || input.Contains('-'))
        {
            return string.Concat(
                input.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant())
            );
        }
        
        // Si es camelCase, convertir a PascalCase
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
```

## Función write_to_file

### ScriptOutputWriter

```csharp
namespace Msi.TemplateCodeGenerator.Services.Templates;

/// <summary>
/// Gestiona la escritura de outputs desde scripts Scriban.
/// </summary>
public sealed class ScriptOutputWriter
{
    private readonly IFileService _fileService;
    private readonly IProjectContext _projectContext;
    private readonly ILogger<ScriptOutputWriter> _logger;
    
    public ScriptOutputWriter(
        IFileService fileService,
        IProjectContext projectContext,
        ILogger<ScriptOutputWriter> logger)
    {
        _fileService = fileService;
        _projectContext = projectContext;
        _logger = logger;
    }
    
    /// <summary>
    /// Escribe contenido a un fichero.
    /// </summary>
    /// <param name="relativePath">Ruta relativa a la raíz del proyecto.</param>
    /// <param name="content">Contenido a escribir.</param>
    public async Task WriteToFile(string relativePath, string content)
    {
        string projectPath = _projectContext.CurrentProject?.FolderPath 
            ?? throw new InvalidOperationException("No project is currently open");
        
        string fullPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));
        
        // Validar que la ruta está dentro del proyecto
        if (!fullPath.StartsWith(projectPath))
        {
            throw new InvalidOperationException(
                $"Output path '{relativePath}' is outside the project directory");
        }
        
        // Crear directorio si no existe
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Escribir fichero
        await _fileService.WriteTextAsync(fullPath, content);
        
        _logger.LogInformation("Script wrote to {Path}", relativePath);
    }
}
```

## Motor de scripts

### IScriptEngine

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Motor de ejecución de scripts Scriban.
/// </summary>
public interface IScriptEngine
{
    /// <summary>
    /// Ejecuta un script Scriban.
    /// </summary>
    /// <param name="scriptContent">Contenido del script.</param>
    /// <param name="scriptPath">Ruta del script (para mensajes de error).</param>
    /// <returns>Resultado de la ejecución.</returns>
    Task<ScriptExecutionResult> ExecuteAsync(string scriptContent, string scriptPath);
}
```

### ScriptExecutionResult

```csharp
namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Resultado de la ejecución de un script Scriban.
/// </summary>
public sealed class ScriptExecutionResult
{
    /// <summary>
    /// Indica si la ejecución fue exitosa.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Mensajes de error (si Success es false).
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Outputs generados por write_to_file.
    /// </summary>
    public List<ScriptOutput> Outputs { get; set; } = new();
}

/// <summary>
/// Representa un output generado por write_to_file.
/// </summary>
public sealed class ScriptOutput
{
    /// <summary>
    /// Ruta relativa del fichero generado.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Contenido generado.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
```

### ScriptEngine (implementación)

```csharp
namespace Msi.TemplateCodeGenerator.Services.Templates;

internal sealed class ScriptEngine : IScriptEngine
{
    private readonly ScriptOutputWriter _outputWriter;
    private readonly ILogger<ScriptEngine> _logger;
    
    public ScriptEngine(
        ScriptOutputWriter outputWriter,
        ILogger<ScriptEngine> logger)
    {
        _outputWriter = outputWriter;
        _logger = logger;
    }
    
    public async Task<ScriptExecutionResult> ExecuteAsync(string scriptContent, string scriptPath)
    {
        ScriptExecutionResult result = new();
        
        try
        {
            // Parsear template
            Template template = Template.Parse(scriptContent, scriptPath);
            
            if (template.HasErrors)
            {
                result.Success = false;
                result.Errors.AddRange(template.Messages.Select(m => m.Message));
                return result;
            }
            
            // Crear contexto
            TemplateContext context = new();
            
            // Registrar helpers
            ScriptObject helpers = new();
            helpers.Import(typeof(ScriptHelpers));
            context.PushGlobal(helpers);
            
            // Registrar write_to_file
            List<ScriptOutput> outputs = new();
            context.Import("write_to_file", new Func<string, string, Task>(async (path, content) =>
            {
                outputs.Add(new ScriptOutput { Path = path, Content = content });
                await _outputWriter.WriteToFile(path, content);
            }));
            
            // Renderizar
            string rendered = template.Render(context);
            
            result.Success = true;
            result.Outputs = outputs;
            
            _logger.LogInformation("Script {Path} executed successfully with {Count} outputs", 
                scriptPath, outputs.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error executing script {Path}", scriptPath);
        }
        
        return result;
    }
}
```

## Registro en DI

```csharp
// En DependencyInjection.cs
services.AddSingleton<ScriptOutputWriter>();
services.AddSingleton<IScriptEngine, ScriptEngine>();
```

## Integración con ITemplatesService

```csharp
// En TemplatesService
public sealed class TemplatesService : ITemplatesService
{
    private readonly IScriptEngine _scriptEngine;
    private readonly IFileService _fileService;
    private readonly ILogger<TemplatesService> _logger;
    
    public TemplatesService(
        IScriptEngine scriptEngine,
        IFileService fileService,
        ILogger<TemplatesService> logger)
    {
        _scriptEngine = scriptEngine;
        _fileService = fileService;
        _logger = logger;
    }
    
    public async Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath)
    {
        string scriptContent = await _fileService.ReadTextAsync(scriptPath);
        return await _scriptEngine.ExecuteAsync(scriptContent, scriptPath);
    }
    
    public async Task ExecuteAllScriptsAsync()
    {
        // Obtener todos los scripts del proyecto
        // (pendiente de definir cómo se obtienen los scripts)
        IEnumerable<string> scriptPaths = GetScriptPaths();
        
        foreach (string scriptPath in scriptPaths)
        {
            ScriptExecutionResult result = await ExecuteScriptAsync(scriptPath);
            
            if (!result.Success)
            {
                _logger.LogWarning("Script {Path} failed: {Errors}", 
                    scriptPath, string.Join(", ", result.Errors));
            }
        }
    }
    
    private IEnumerable<string> GetScriptPaths()
    {
        // Pendiente de implementar: obtener scripts del proyecto
        return Enumerable.Empty<string>();
    }
}
```

## Ejemplo de script

### workflow-dto.scriban

```scriban
{{ for element in get_all_elements() }}
  {{ if element.Type == "Workflow" }}
    {{ capture content }}
namespace {{ element.Get<string>("Namespace") }};

public class {{ element.Name }}Dto
{
    {{ for prop in element.Properties }}
      {{ if prop.Type == "string" }}
    public string {{ prop.Name | pascal_case }} { get; set; }
      {{ else if prop.Type == "int" }}
    public int {{ prop.Name | pascal_case }} { get; set; }
      {{ else if prop.Type == "bool" }}
    public bool {{ prop.Name | pascal_case }} { get; set; }
      {{ end }}
    {{ end }}
}
    {{ end }}
    {{ write_to_file("src/" + element.Get<string>("Namespace") + "/" + element.Name + "Dto.cs", content) }}
  {{ end }}
{{ end }}
```

## Testing

### Unit tests

```csharp
public class ScriptHelpersTests
{
    [Fact]
    public void PascalCase_WithSnakeCase_ConvertsCorrectly()
    {
        // Act
        string result = ScriptHelpers.PascalCase("order_processing");
        
        // Assert
        result.ShouldBe("OrderProcessing");
    }
    
    [Fact]
    public void PascalCase_WithCamelCase_ConvertsCorrectly()
    {
        // Act
        string result = ScriptHelpers.PascalCase("orderProcessing");
        
        // Assert
        result.ShouldBe("OrderProcessing");
    }
    
    [Fact]
    public void CamelCase_WithPascalCase_ConvertsCorrectly()
    {
        // Act
        string result = ScriptHelpers.CamelCase("OrderProcessing");
        
        // Assert
        result.ShouldBe("orderProcessing");
    }
}

public class ScriptEngineTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidScript_ReturnsSuccess()
    {
        // Arrange
        string script = "{{ get_all_elements().Count() }}";
        var engine = CreateEngine();
        
        // Act
        var result = await engine.ExecuteAsync(script, "test.scriban");
        
        // Assert
        result.Success.ShouldBeTrue();
    }
    
    [Fact]
    public async Task ExecuteAsync_WithSyntaxError_ReturnsErrors()
    {
        // Arrange
        string script = "{{ invalid syntax }}";
        var engine = CreateEngine();
        
        // Act
        var result = await engine.ExecuteAsync(script, "test.scriban");
        
        // Assert
        result.Success.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
    }
    
    private ScriptEngine CreateEngine()
    {
        // Mock dependencies
        var outputWriter = new ScriptOutputWriter(/* mocks */);
        var logger = new Logger<ScriptEngine>(new LoggerFactory());
        return new ScriptEngine(outputWriter, logger);
    }
}
```

## Criterios de aceptación

- [ ] `dotnet build` sin errores
- [ ] Script de prueba con `get_all_elements()` retorna los Elements esperados
- [ ] Script de prueba con `write_to_file` genera fichero en disco
- [ ] Script con error de sintaxis → error controlado (no crash)
- [ ] Test unitario: helpers retornan datos correctos
- [ ] Test unitario: `PascalCase()` funciona con snake_case, camelCase, PascalCase
- [ ] Test unitario: `write_to_file` escribe ficheros correctamente
- [ ] `IScriptEngine` registrado en DI y accesible desde servicios

## Ejemplo de uso

```csharp
// En un servicio o ViewModel
public class SomeService
{
    private readonly IScriptEngine _scriptEngine;
    
    public SomeService(IScriptEngine scriptEngine)
    {
        _scriptEngine = scriptEngine;
    }
    
    public async Task GenerateCodeAsync(string scriptPath)
    {
        string scriptContent = await File.ReadAllTextAsync(scriptPath);
        ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(scriptContent, scriptPath);
        
        if (result.Success)
        {
            Console.WriteLine($"Generated {result.Outputs.Count} files");
        }
        else
        {
            Console.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
        }
    }
}
```

## Pendiente de definir

- Detalles de encoding y line endings en write_to_file
- Cómo se obtienen los scripts del proyecto (Fase 3)
- Preview en UI
- Manejo de errores en UI
- Timeout de ejecución de scripts
