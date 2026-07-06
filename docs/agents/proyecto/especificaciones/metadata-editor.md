# Especificación técnica: Editor de Metadata

> Detalles de implementación del editor de metadata JSON con preview en tiempo real.
> Referencia: ADR-003

## Arquitectura

### Jerarquía de editores

```
BaseTextEditorViewModel (abstracta)
  ├── TemplateEditorShellViewModel (Scriban + preview renderizado)
  └── MetadataEditorShellViewModel (JSON + preview parseado + defaults)
```

### Componentes

```
MetadataEditorShellView (AXAML)
  ├── Panel izquierdo: TextBox editable (JSON)
  └── Panel derecho: TextBlock readonly (preview)

MetadataEditorShellViewModel
  ├── Hereda: Load/Save, dirty tracking, ICloseAware, ICommandRoute
  ├── OnContentChangedCore(): validación JSON + merge defaults
  └── PreviewContent: JSON formateado con defaults aplicados
```

## MetadataEditorShellViewModel

### Declaración

```csharp
internal sealed partial class MetadataEditorShellViewModel : BaseTextEditorViewModel
{
    [ObservableProperty]
    private string _previewContent = string.Empty;
    
    [ObservableProperty]
    private bool _hasError;
    
    private readonly IMetadataService _metadataService;
    private CancellationTokenSource? _previewCts;
    
    public MetadataEditorShellViewModel(
        IFileService fileService,
        IDialogService dialogService,
        IMetadataService metadataService,
        ILogger<MetadataEditorShellViewModel> logger)
        : base(fileService, dialogService, logger)
    {
        _metadataService = metadataService;
    }
}
```

### Preview con debounce

```csharp
protected override void OnContentChangedCore(string value)
{
    // Cancelar preview previo
    _previewCts?.Cancel();
    _previewCts?.Dispose();
    _previewCts = new CancellationTokenSource();
    
    CancellationToken token = _previewCts.Token;
    
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(500, token); // Debounce 500ms
            UpdatePreview(value, token);
        }
        catch (TaskCanceledException)
        {
            // Cancelación legítima por nueva pulsación
        }
    }, token);
}

private void UpdatePreview(string jsonContent, CancellationToken token)
{
    try
    {
        // 1. Parsear JSON
        MetadataFile file = JsonSerializer.Deserialize<MetadataFile>(jsonContent);
        token.ThrowIfCancellationRequested();
        
        // 2. Determinar categoría desde el header
        string category = file.Header.Category;
        string? defaultsFileName = file.Header.Defaults;
        
        // 3. Cargar defaults (si existen)
        object? defaults = null;
        if (!string.IsNullOrEmpty(defaultsFileName))
        {
            string defaultsPath = Path.Combine(
                Path.GetDirectoryName(FilePath)!,
                defaultsFileName);
            
            if (File.Exists(defaultsPath))
            {
                string defaultsJson = File.ReadAllText(defaultsPath);
                defaults = DeserializeMetadata(defaultsJson, category);
            }
        }
        
        token.ThrowIfCancellationRequested();
        
        // 4. Aplicar merge
        object merged = MergeWithDefaults(file.Data, defaults, category);
        
        // 5. Serializar a JSON formateado para el preview
        string preview = JsonSerializer.Serialize(merged, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // 6. Actualizar UI en el thread principal
        Dispatcher.UIThread.Post(() =>
        {
            PreviewContent = preview;
            HasError = false;
        });
    }
    catch (Exception ex)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PreviewContent = $"Error: {ex.Message}";
            HasError = true;
        });
    }
}
```

### Deserialización dinámica por categoría

```csharp
private object? DeserializeMetadata(string json, string category)
{
    Type? targetType = _metadataRegistry.GetCategoryType(category);
    if (targetType == null)
        return null;
    
    return JsonSerializer.Deserialize(json, targetType);
}

private object MergeWithDefaults(object data, object? defaults, string category)
{
    if (defaults == null)
        return data;
    
    // Merge de un nivel: propiedades simples
    Type type = data.GetType();
    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    
    foreach (PropertyInfo prop in properties)
    {
        if (IsCollectionType(prop.PropertyType))
            continue;
        
        object? dataValue = prop.GetValue(data);
        object? defaultsValue = prop.GetValue(defaults);
        
        if (IsDefaultValue(dataValue, prop.PropertyType) && defaultsValue != null)
        {
            prop.SetValue(data, defaultsValue);
        }
    }
    
    return data;
}
```

### MetadataFile (wrapper para deserialización)

```csharp
internal sealed class MetadataFile
{
    public MetadataFileHeader Header { get; set; } = new();
    public JsonElement Data { get; set; }
}
```

**Nota:** `Data` se deserializa como `JsonElement` para permitir deserialización dinámica posterior según la categoría.

## MetadataEditorShellView

### Layout

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Msi.TemplateCodeGenerator.UI.Views.MetadataEditor.MetadataEditorShellView">
  <Grid ColumnDefinitions="*,4,*">
    <!-- Editor JSON (izquierda) -->
    <TextBox Grid.Column="0"
             Text="{Binding Content}"
             AcceptsReturn="True"
             AcceptsTab="True"
             FontFamily="Consolas,Menlo,monospace"
             FontSize="14"
             TextWrapping="NoWrap" />
    
    <!-- Splitter -->
    <GridSplitter Grid.Column="1" 
                  Background="{DynamicResource SystemControlForegroundBaseMediumLowBrush}" />
    
    <!-- Preview (derecha) -->
    <Panel Grid.Column="2">
      <!-- Preview normal -->
      <TextBox Text="{Binding PreviewContent}"
               IsReadOnly="True"
               FontFamily="Consolas,Menlo,monospace"
               FontSize="14"
               TextWrapping="Wrap"
               IsVisible="{Binding !HasError}" />
      
      <!-- Preview con error -->
      <TextBlock Text="{Binding PreviewContent}"
                 Foreground="Red"
                 TextWrapping="Wrap"
                 Margin="8"
                 IsVisible="{Binding HasError}" />
    </Panel>
  </Grid>
</UserControl>
```

### Code-behind

```csharp
internal partial class MetadataEditorShellView : UserControl
{
    public MetadataEditorShellView()
    {
        InitializeComponent();
    }
}
```

## Modificaciones a servicios existentes

### NavigationService

```csharp
public void OpenFile(string filePath)
{
    string documentId = $"File_{filePath}";
    
    // Buscar si ya está abierto
    IDockable? existing = FindDocument(documentId);
    if (existing != null)
    {
        _factory.SetActiveDockable(existing);
        return;
    }
    
    // Determinar editor por extensión
    string extension = Path.GetExtension(filePath).ToLowerInvariant();
    BaseViewModel editorVM = extension switch
    {
        ".scriban" => CreateTemplateEditor(filePath),
        ".json" when IsMetadataFile(filePath) => CreateMetadataEditor(filePath),
        _ => CreateTextEditor(filePath) // futuro: editor genérico
    };
    
    // Crear documento dockable
    Document document = new()
    {
        Id = documentId,
        Title = Path.GetFileName(filePath),
        Context = editorVM,
        CanClose = true
    };
    
    _factory.AddDockable(_documentsPane, document);
    _factory.ActiveDockable = document;
}

private bool IsMetadataFile(string filePath)
{
    // Detectar si el fichero está en la carpeta metadata/
    return filePath.Contains(Path.DirectorySeparatorChar + "metadata" + Path.DirectorySeparatorChar);
}

private BaseViewModel CreateMetadataEditor(string filePath)
{
    IServiceScope scope = _serviceProvider.CreateScope();
    MetadataEditorShellViewModel vm = scope.ServiceProvider.GetRequiredService<MetadataEditorShellViewModel>();
    vm.LoadFileAsync(filePath).ContinueWith(_ => { }, TaskScheduler.Default);
    
    _documentScopes[$"File_{filePath}"] = scope;
    return vm;
}
```

### ViewLocator

```csharp
public Control? Build(object? viewModel)
{
    return viewModel switch
    {
        TemplateEditorShellViewModel => new TemplateEditorShellView(),
        MetadataEditorShellViewModel => new MetadataEditorShellView(),
        _ => BuildByConvention(viewModel)
    };
}
```

### DependencyInjection

```csharp
// Añadir al registro de servicios
services.AddScoped<MetadataEditorShellViewModel>();
```

### ProjectExplorerShellViewModel

```csharp
[RelayCommand]
private void OpenFile(FileEntryViewModel fileEntry)
{
    _logger.LogInformation("[UI] Command: OpenFile '{FilePath}'", fileEntry.RelativePath);
    
    string fullPath = Path.Combine(_context.CurrentProject!.FolderPath, fileEntry.RelativePath);
    _navigationService.OpenFile(fullPath);
}
```

**Nota:** El `ProjectExplorer` ya invoca `INavigationService.OpenFile()`. La detección por extensión se hace dentro de `NavigationService`, no en el `ProjectExplorer`.

## Registro DI

```csharp
// En DependencyInjection.cs
services.AddScoped<TemplateEditorShellViewModel>();
services.AddScoped<MetadataEditorShellViewModel>();
```

## Testing

### Unit tests

- **UpdatePreview**: JSON válido sin defaults → preview muestra JSON formateado.
- **UpdatePreview**: JSON válido con defaults → preview muestra merge.
- **UpdatePreview**: JSON inválido → preview muestra error.
- **UpdatePreview**: JSON con categoría no registrada → preview muestra JSON sin merge.

### Integration tests

- **OpenFile**: Abrir `.json` en `metadata/` → se abre `MetadataEditorShellView`.
- **OpenFile**: Abrir `.scriban` → se abre `TemplateEditorShellView`.
- **Save**: Editar JSON, guardar, recargar → contenido persistido correctamente.

## Consideraciones de UI

### Indicadores visuales

- **Panel izquierdo**: Texto editable con fuente monoespaciada.
- **Panel derecho**: 
  - Si no hay error: JSON formateado con defaults aplicados (solo lectura).
  - Si hay error: Texto en rojo con mensaje de error.
- **Splitter**: Redimensionable entre los dos paneles.

### Rendimiento

- **Debounce**: 500ms entre la última pulsación y el preview.
- **Cancelación**: Si el usuario sigue escribiendo, se cancela el preview previo.
- **Thread**: El parsing y merge se hacen en background thread. La actualización de UI se hace en `Dispatcher.UIThread`.

## Futuras extensiones

### Syntax highlighting

- Integrar AvaloniaEdit o similar para resaltado de sintaxis JSON.
- Configurar gramática JSON automáticamente.

### Intellisense

- Autocompletado basado en el schema de la categoría.
- Sugerencias de propiedades basadas en el POCO registrado.

### Validación cross-reference

- Validar que las referencias entre categorías son válidas.
- Ej: si un ElementId referencia un Element, validar que el Element existe.

### Editor visual

- Formulario visual para editar propiedades individuales.
- Toggle entre modo texto (JSON) y modo visual (formulario).
