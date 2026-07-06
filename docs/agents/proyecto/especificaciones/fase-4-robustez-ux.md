# Especificación técnica: Fase 4 - Robustez y UX

> Mejoras de robustez y experiencia de usuario: tolerancia a errores, re-carga automática, documentación.
> Referencia: ADR-005, Fase 4

## Objetivo

Pulir la experiencia y hacer el sistema resistente: marcado de errores en UI, re-carga automática, documentación de schemas y helpers.

## Alcance

### Incluye
- Marcar ficheros con error en ProjectExplorer (aspa roja)
- Re-carga automática al modificar fichero en disco (FileWatcher)
- Documentación de schemas (markdown por tipo de Element)
- Documentación de helpers disponibles

### No incluye
- IntelliSense en editor
- Autocompletado
- Language Server

### Dependencias
- **Fase 3 completada**: UI básica funcional

## Marcado de errores en ProjectExplorer

### FileEntryViewModel (actualizado)

```csharp
namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

internal sealed partial class FileEntryViewModel : BaseViewModel
{
    public string FullPath { get; }
    public string RelativePath { get; }
    public FileType FileType { get; }
    
    [ObservableProperty]
    private bool _hasError;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    public string FileName => Path.GetFileName(FullPath);
    
    public string Icon => FileType switch
    {
        FileType.Data => "📄",
        FileType.Script => "📝",
        _ => "📄"
    };
    
    public string StatusIcon => HasError ? "❌" : Icon;
    
    public FileEntryViewModel(string fullPath, string relativePath, FileType fileType)
    {
        FullPath = fullPath;
        RelativePath = relativePath;
        FileType = fileType;
    }
    
    public void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        OnPropertyChanged(nameof(StatusIcon));
    }
    
    public void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(StatusIcon));
    }
}
```

### ProjectExplorerShellViewModel (actualizado)

```csharp
internal sealed partial class ProjectExplorerShellViewModel : BaseViewModel
{
    // ... código existente ...
    
    private async Task LoadProjectFilesAsync()
    {
        Files.Clear();
        Scripts.Clear();
        
        string projectPath = _projectContext.CurrentProject?.FolderPath ?? string.Empty;
        
        if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            return;
        
        // Cargar ficheros .json (datos)
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(projectPath, "*.json", SearchOption.AllDirectories);
        foreach (string jsonFile in jsonFiles)
        {
            string relativePath = Path.GetRelativePath(projectPath, jsonFile);
            var fileEntry = new FileEntryViewModel(jsonFile, relativePath, FileType.Data);
            Files.Add(fileEntry);
        }
        
        // Cargar ficheros .scriban (scripts)
        IEnumerable<string> scribanFiles = Directory.EnumerateFiles(projectPath, "*.scriban", SearchOption.AllDirectories);
        foreach (string scribanFile in scribanFiles)
        {
            string relativePath = Path.GetRelativePath(projectPath, scribanFile);
            var scriptEntry = new FileEntryViewModel(scribanFile, relativePath, FileType.Script);
            Scripts.Add(scriptEntry);
        }
        
        // Marcar errores de carga
        await MarkLoadErrorsAsync();
        
        _logger.LogInformation("Loaded {FileCount} data files and {ScriptCount} scripts", 
            Files.Count, Scripts.Count);
    }
    
    private async Task MarkLoadErrorsAsync()
    {
        // Marcar errores de JSONs
        IReadOnlyList<LoadError> loadErrors = _elementCatalog.GetLoadErrors();
        
        foreach (LoadError error in loadErrors)
        {
            var fileEntry = Files.FirstOrDefault(f => f.FullPath == error.FilePath);
            if (fileEntry != null)
            {
                fileEntry.SetError(error.Message);
            }
        }
        
        // Validar scripts (parseo básico)
        foreach (var scriptEntry in Scripts)
        {
            try
            {
                string content = await _fileService.ReadTextAsync(scriptEntry.FullPath);
                Template template = Template.Parse(content, scriptEntry.FullPath);
                
                if (template.HasErrors)
                {
                    string errorMessage = string.Join(", ", template.Messages.Select(m => m.Message));
                    scriptEntry.SetError(errorMessage);
                }
            }
            catch (Exception ex)
            {
                scriptEntry.SetError(ex.Message);
            }
        }
    }
}
```

### ProjectExplorerShellView.axaml (actualizado)

```xml
<TreeView Grid.Row="0" ItemsSource="{Binding Scripts}" Margin="0,30,0,0">
    <TreeView.ItemTemplate>
        <TreeDataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{Binding StatusIcon}" />
                <TextBlock Text="{Binding FileName}" />
                <TextBlock Text="❓" 
                           IsVisible="{Binding HasError}"
                           ToolTip.Tip="{Binding ErrorMessage}" />
            </StackPanel>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

## FileWatcher: re-carga automática

### IFileWatcherService

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio de vigilancia de cambios en ficheros.
/// </summary>
public interface IFileWatcherService
{
    /// <summary>
    /// Evento disparado cuando un fichero cambia.
    /// </summary>
    event EventHandler<FileChangedEventArgs>? FileChanged;
    
    /// <summary>
    /// Inicia la vigilancia en un directorio.
    /// </summary>
    void StartWatching(string directoryPath);
    
    /// <summary>
    /// Detiene la vigilancia.
    /// </summary>
    void StopWatching();
}

public sealed class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public FileChangeType ChangeType { get; set; }
}

public enum FileChangeType
{
    Created,
    Changed,
    Deleted
}
```

### FileWatcherService (implementación)

```csharp
namespace Msi.TemplateCodeGenerator.Services;

internal sealed class FileWatcherService : IFileWatcherService, IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly ILogger<FileWatcherService> _logger;
    
    public event EventHandler<FileChangedEventArgs>? FileChanged;
    
    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
    }
    
    public void StartWatching(string directoryPath)
    {
        StopWatching();
        
        _watcher = new FileSystemWatcher(directoryPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
        
        _logger.LogInformation("Started watching {Path}", directoryPath);
    }
    
    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileCreated;
            _watcher.Deleted -= OnFileDeleted;
            _watcher.Dispose();
            _watcher = null;
            
            _logger.LogInformation("Stopped watching");
        }
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = FileChangeType.Changed
        });
    }
    
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = FileChangeType.Created
        });
    }
    
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = FileChangeType.Deleted
        });
    }
    
    public void Dispose()
    {
        StopWatching();
    }
}
```

### Integración con ProjectExplorerShellViewModel

```csharp
internal sealed partial class ProjectExplorerShellViewModel : BaseViewModel, IDisposable
{
    private readonly IFileWatcherService _fileWatcher;
    private bool _disposed;
    
    public ProjectExplorerShellViewModel(
        IProjectContext projectContext,
        IProjectService projectService,
        IElementCatalog elementCatalog,
        IFileService fileService,
        IFileWatcherService fileWatcher,
        IMessenger messenger,
        INavigationService navigationService,
        ILogger<ProjectExplorerShellViewModel> logger)
    {
        // ... inicialización existente ...
        
        _fileWatcher = fileWatcher;
        _fileWatcher.FileChanged += OnFileChanged;
    }
    
    private async void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        // Solo procesar .json y .scriban
        string extension = Path.GetExtension(e.FilePath).ToLowerInvariant();
        if (extension != ".json" && extension != ".scriban")
            return;
        
        _logger.LogInformation("File changed: {Path} ({ChangeType})", e.FilePath, e.ChangeType);
        
        // Debounce: esperar 500ms antes de recargar
        await Task.Delay(500);
        
        await LoadProjectFilesAsync();
    }
    
    private async Task LoadProjectFilesAsync()
    {
        // ... código existente ...
        
        // Recargar catálogo si hay cambios en JSONs
        await _elementCatalog.ReloadAsync();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _fileWatcher.FileChanged -= OnFileChanged;
            _disposed = true;
        }
    }
}
```

### Integración con ProjectService

```csharp
public sealed class ProjectService : IProjectService
{
    private readonly IFileWatcherService _fileWatcher;
    
    public async Task OpenProjectAsync(string projectPath)
    {
        // ... código existente ...
        
        // Iniciar FileWatcher
        _fileWatcher.StartWatching(projectPath);
    }
    
    public async Task CloseProjectAsync()
    {
        // ... código existente ...
        
        // Detener FileWatcher
        _fileWatcher.StopWatching();
    }
}
```

## Documentación de schemas

### Estructura de documentación

```
docs/
├── schemas/
│   ├── Workflow.md
│   ├── Vista.md
│   └── WorkflowId.md
└── helpers/
    └── script-helpers.md
```

### Ejemplo: docs/schemas/Workflow.md

```markdown
# Schema: Workflow

## Descripción
Define un flujo de trabajo automatizado con actividades y transiciones.

## Propiedades obligatorias

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `Id` | string | Identificador único del workflow |
| `Name` | string | Nombre legible del workflow |
| `Type` | string | Debe ser "Workflow" |
| `Namespace` | string | Namespace C# donde se generará el código |

## Propiedades opcionales

| Nombre | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Description` | string | "" | Descripción del workflow |
| `MaxRetries` | int | 3 | Número máximo de reintentos en caso de error |
| `IsActive` | bool | true | Indica si el workflow está activo |
| `Activities` | array | [] | Lista de actividades del workflow |

## Ejemplo JSON

```json
{
  "Id": "wf-001",
  "Name": "OrderProcessing",
  "Type": "Workflow",
  "Properties": [
    { "Name": "Namespace", "Type": "string", "Value": "MyApp.Workflows" },
    { "Name": "Description", "Type": "string", "Value": "Procesa órdenes" },
    { "Name": "MaxRetries", "Type": "int", "Value": 3 },
    { "Name": "IsActive", "Type": "bool", "Value": true }
  ]
}
```

## Uso en scripts

```scriban
{{ for element in get_all_elements() }}
  {{ if element.Type == "Workflow" }}
    Workflow: {{ element.Name }}
    Namespace: {{ element.Get<string>("Namespace") }}
    MaxRetries: {{ element.Get<int>("MaxRetries") }}
  {{ end }}
{{ end }}
```
```

## Documentación de helpers

### docs/helpers/script-helpers.md

```markdown
# Script Helpers

Funciones C# disponibles en scripts Scriban.

## get_all_elements()

Retorna todos los Elements del catálogo.

**Retorna:** `IEnumerable<Element>`

**Ejemplo:**
```scriban
{{ for element in get_all_elements() }}
  {{ element.Name }}
{{ end }}
```

## get_elements_by_type(type)

Retorna todos los Elements de un tipo específico.

**Parámetros:**
- `type` (string): Tipo del Element (ej: "Workflow")

**Retorna:** `IEnumerable<Element>`

**Ejemplo:**
```scriban
{{ for element in get_elements_by_type("Workflow") }}
  {{ element.Name }}
{{ end }}
```

## pascal_case(input)

Convierte un string a PascalCase.

**Parámetros:**
- `input` (string): String de entrada

**Retorna:** `string`

**Ejemplo:**
```scriban
{{ "order_processing" | pascal_case }}  → OrderProcessing
{{ "orderProcessing" | pascal_case }}   → OrderProcessing
```

## camel_case(input)

Convierte un string a camelCase.

**Parámetros:**
- `input` (string): String de entrada

**Retorna:** `string`

**Ejemplo:**
```scriban
{{ "OrderProcessing" | camel_case }}    → orderProcessing
{{ "order_processing" | camel_case }}   → orderProcessing
```

## write_to_file(path, content)

Escribe contenido a un fichero.

**Parámetros:**
- `path` (string): Ruta relativa a la raíz del proyecto
- `content` (string): Contenido a escribir

**Ejemplo:**
```scriban
{{ write_to_file("src/MyClass.cs", "public class MyClass {}") }}
```
```

## Registro en DI

```csharp
// En DependencyInjection.cs
services.AddSingleton<IFileWatcherService, FileWatcherService>();
```

## Testing

### Unit tests

```csharp
public class FileWatcherServiceTests
{
    [Fact]
    public void StartWatching_WhenFileChanged_RaisesEvent()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        var service = new FileWatcherService(new Logger<FileWatcherService>(new LoggerFactory()));
        bool eventRaised = false;
        
        service.FileChanged += (sender, e) => eventRaised = true;
        
        // Act
        service.StartWatching(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "content");
        
        // Esperar a que FileSystemWatcher dispare el evento
        Thread.Sleep(1000);
        
        // Assert
        eventRaised.ShouldBeTrue();
        
        // Cleanup
        service.Dispose();
        Directory.Delete(tempDir, recursive: true);
    }
}

public class FileEntryViewModelTests
{
    [Fact]
    public void SetError_SetsHasErrorAndErrorMessage()
    {
        // Arrange
        var viewModel = new FileEntryViewModel("/path/to/file.json", "file.json", FileType.Data);
        
        // Act
        viewModel.SetError("Invalid JSON");
        
        // Assert
        viewModel.HasError.ShouldBeTrue();
        viewModel.ErrorMessage.ShouldBe("Invalid JSON");
        viewModel.StatusIcon.ShouldBe("❌");
    }
    
    [Fact]
    public void ClearError_ClearsHasErrorAndErrorMessage()
    {
        // Arrange
        var viewModel = new FileEntryViewModel("/path/to/file.json", "file.json", FileType.Data);
        viewModel.SetError("Invalid JSON");
        
        // Act
        viewModel.ClearError();
        
        // Assert
        viewModel.HasError.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBeEmpty();
        viewModel.StatusIcon.ShouldBe("📄");
    }
}
```

## Criterios de aceptación

- [ ] `dotnet build` sin errores
- [ ] Corromper JSON → aspa roja en ProjectExplorer
- [ ] Corregir JSON → aspa desaparece, Element disponible
- [ ] Editar script en disco → preview se actualiza automáticamente
- [ ] Documentación de schemas accesible en `docs/schemas/`
- [ ] Documentación de helpers accesible en `docs/helpers/`
- [ ] FileWatcher detecta cambios en ficheros .json y .scriban
- [ ] Re-carga automática con debounce de 500ms
- [ ] ToolTip muestra mensaje de error al pasar el mouse sobre el aspa roja

## Flujo de usuario

1. **Abrir proyecto**: ProjectExplorer carga scripts y datos
2. **Error en JSON**: Aspa roja aparece en el fichero con error
3. **Corregir JSON**: Aspa desaparece automáticamente (FileWatcher)
4. **Editar script en disco**: Preview se actualiza automáticamente
5. **Consultar documentación**: Usuario abre `docs/schemas/Workflow.md` para ver schema

## Consideraciones de rendimiento

- **Debounce**: 500ms para evitar recargas excesivas
- **FileSystemWatcher**: Puede disparar múltiples eventos para un solo cambio
- **Validación de scripts**: Parseo básico al cargar (puede ser lento con muchos scripts)

## Pendiente de definir

- IntelliSense en editor
- Autocompletado basado en schemas
- Language Server para Scriban
- Validación de schemas en tiempo real
