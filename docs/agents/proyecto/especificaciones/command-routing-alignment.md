# Especificación técnica: Alineación del Command Routing

## Propósito

Documentar la clasificación de operaciones (global vs contextual) y alinear la implementación existente con la arquitectura definida en ADR-001.

## Referencia

- ADR: `docs/agents/proyecto/adr/ADR-001-command-routing.md`
- Especificación de Command Routing: `docs/agents/proyecto/especificaciones/command-routing.md`

## 1. Clasificación de operaciones

### Criterio

¿La operación necesita saber qué documento tiene foco para ejecutarse correctamente?
- **Sí** → Contextual. Routing obligatorio vía `ICommandRegistry`.
- **No** → Global. Llamada directa a servicios.

### Tabla de clasificación

| Operación | Categoría | Patrón | Servicio | Razón |
|---|---|---|---|---|
| `NewProject` | Global | Llamada directa | `IProjectService` | No depende del documento activo |
| `OpenProject` | Global | Llamada directa | `IProjectService` | No depende del documento activo |
| `CloseProject` | Global | Llamada directa | `IProjectService` | No depende del documento activo |
| `SaveProject` | Global | Llamada directa | `IProjectService` | No depende del documento activo |
| `SaveProjectAs` | Global | Llamada directa | `IProjectService` | No depende del documento activo |
| `Save` (editor) | **Contextual** | `ICommandRegistry` → `ICommandRoute` | `IFileService` | Depende del dockable con foco |
| `OpenFile` (desde tree) | Global | Llamada directa | `INavigationService` | No depende del documento activo |
| `RefreshFiles` | Global | Llamada directa | `IProjectService` | Operación de shell |
| `Exit` | Global | `IApp.Shutdown()` | — | Operación de shell |

### Regla

**Prohibido** crear comandos contextuales para operaciones globales. Si una operación no depende del documento activo, debe llamarse directamente al servicio correspondiente.

**Prohibido** que el Shell invoque servicios de dominio directamente para comandos contextuales. El flujo obligatorio es:

```
Shell → ICommandRegistry → ICommandContext.ActiveRoute → ICommandRoute.ExecuteAsync() → Servicio
```

### Ejemplo correcto: Save contextual

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync()
{
    _logger.LogInformation("[UI] Command: Save (contextual)");
    await _commandRegistry.ExecuteAsync("Save");
}

private bool CanSave() => _commandRegistry.CanExecute("Save");
```

### Ejemplo correcto: SaveProject global

```csharp
[RelayCommand]
private async Task SaveProjectAsync()
{
    _logger.LogInformation("[UI] Command: SaveProject");
    await _projectService.SaveProjectAsync();
}
```

### Ejemplo incorrecto: Shell acoplado al editor

```csharp
[RelayCommand]
private async Task SaveAsync()
{
    // INCORRECTO: El Shell no debe conocer IFileService ni el editor activo
    await _fileService.WriteTextAsync(editorVm.FilePath, editorVm.Content);
}
```

## 2. Fix: MetadataEditorShellViewModel — Extraer business logic a servicio

### Problema

`MetadataEditorShellViewModel.UpdatePreview()` (líneas 71-115) contiene:
- Deserialización JSON (`JsonSerializer.Deserialize<MetadataFile>`)
- Acceso directo al sistema de ficheros (`File.Exists()`, `File.ReadAllText()`) bypass de `IFileService`
- Algoritmo de merge recursivo (`MergeJsonElements`)

Esto viola la regla MVVM del proyecto: *"ViewModels exponen bindings/commands únicamente. La lógica de negocio va en Services."*

### Solución

Extraer la lógica a `IMetadataService`. La especificación `metadata-editor.md` ya contemplaba este servicio, pero la implementación divergió.

### Interfaz `IMetadataService`

**Ubicación:** `Interfaces/IMetadataService.cs`

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio de procesamiento de metadata JSON.
/// Encargado de parsear, cargar defaults y aplicar merge.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Parsea el JSON, carga defaults si existen, aplica merge y devuelve el resultado formateado.
    /// </summary>
    /// <param name="jsonContent">Contenido JSON del editor.</param>
    /// <param name="editorFilePath">Path del fichero del editor (para resolver defaults relativos).</param>
    /// <returns>Resultado del procesamiento con el preview formateado y flag de error.</returns>
    Task<MetadataPreviewResult> ProcessPreviewAsync(string jsonContent, string editorFilePath);
}
```

### Modelo `MetadataPreviewResult`

**Ubicación:** `Models/MetadataPreviewResult.cs`

```csharp
namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Resultado del procesamiento de preview de metadata.
/// </summary>
public sealed class MetadataPreviewResult
{
    /// <summary>
    /// Contenido formateado para el preview. Si hubo error, contiene el mensaje de error.
    /// </summary>
    public string PreviewContent { get; init; } = string.Empty;

    /// <summary>
    /// Indica si hubo un error durante el procesamiento.
    /// </summary>
    public bool HasError { get; init; }
}
```

### Implementación `MetadataService`

**Ubicación:** `Services/MetadataService.cs`

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Models.Metadata;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Servicio de procesamiento de metadata JSON.
/// Encargado de parsear, cargar defaults y aplicar merge.
/// </summary>
internal sealed class MetadataService(
    IFileService fileService,
    ILogger<MetadataService> logger) : IMetadataService
{
    private readonly IFileService _fileService = fileService;
    private readonly ILogger<MetadataService> _logger = logger;

    /// <inheritdoc/>
    public async Task<MetadataPreviewResult> ProcessPreviewAsync(string jsonContent, string editorFilePath)
    {
        try
        {
            MetadataFile file = JsonSerializer.Deserialize<MetadataFile>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new JsonException("El JSON está vacío o es inválido.");

            string? defaultsFileName = file.Header.Defaults;

            if (!string.IsNullOrEmpty(defaultsFileName) && !string.IsNullOrEmpty(editorFilePath))
            {
                string? directory = Path.GetDirectoryName(editorFilePath);
                if (directory != null)
                {
                    string defaultsPath = Path.Combine(directory, defaultsFileName);

                    if (File.Exists(defaultsPath))
                    {
                        // Usar IFileService en lugar de File.ReadAllText directo
                        string defaultsJson = await _fileService.ReadTextAsync(defaultsPath);
                        MetadataFile defaultsFile = JsonSerializer.Deserialize<MetadataFile>(defaultsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? throw new JsonException("El fichero de defaults es inválido.");

                        MergeJsonElements(file.Data, defaultsFile.Data);
                    }
                }
            }

            string preview = JsonSerializer.Serialize(file.Data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return new MetadataPreviewResult
            {
                PreviewContent = preview,
                HasError = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al procesar preview de metadata");
            return new MetadataPreviewResult
            {
                PreviewContent = $"Error: {ex.Message}",
                HasError = true
            };
        }
    }

    /// <summary>
    /// Merge recursivo de dos JsonElement: los valores de source se aplican sobre target
    /// solo cuando target tiene el valor por defecto (null, string vacío, false, 0).
    /// Los arrays no se mergean, solo los objetos.
    /// </summary>
    private static void MergeJsonElements(JsonElement target, JsonElement source)
    {
        if (target.ValueKind != JsonValueKind.Object || source.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty sourceProp in source.EnumerateObject())
        {
            if (!target.TryGetProperty(sourceProp.Name, out JsonElement targetProp))
                continue;

            if (targetProp.ValueKind == JsonValueKind.Object && sourceProp.Value.ValueKind == JsonValueKind.Object)
            {
                MergeJsonElements(targetProp, sourceProp.Value);
            }
        }
    }
}
```

### Cambios en `MetadataEditorShellViewModel`

**Ubicación:** `src/Msi.TemplateCodeGenerator/UI/Views/MetadataEditor/ViewModels/MetadataEditorShellViewModel.cs`

**Estado actual:** Contiene toda la lógica de procesamiento inline.

**Nuevo estado:** Delega en `IMetadataService`.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Views.MetadataEditor.ViewModels;

/// <summary>
/// ViewModel del editor de metadatos JSON.
/// Hereda el manejo de fichero y contenido de BaseTextEditorViewModel
/// y delega el procesamiento de metadata en IMetadataService.
/// </summary>
internal sealed partial class MetadataEditorShellViewModel(
    IFileService fileService,
    IDialogService dialogService,
    IMetadataService metadataService,
    ILogger<MetadataEditorShellViewModel> logger)
    : BaseTextEditorViewModel(fileService, dialogService, logger)
{
    private readonly ILogger<MetadataEditorShellViewModel> _logger = logger;
    private readonly IMetadataService _metadataService = metadataService;

    [ObservableProperty]
    private string _previewContent = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    private CancellationTokenSource? _debounceCts;

    /// <inheritdoc/>
    protected override void OnContentChangedCore(string value)
    {
        _logger.LogDebug("Contenido JSON cambiado ({CharCount} chars), reiniciando debounce", value.Length);

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        _ = UpdatePreviewWithDebounceAsync(value, token);
    }

    private async Task UpdatePreviewWithDebounceAsync(string content, CancellationToken token)
    {
        try
        {
            await Task.Delay(500, token);

            if (token.IsCancellationRequested)
            {
                _logger.LogDebug("Debounce cancelado por nuevo cambio de contenido");
                return;
            }

            _logger.LogDebug("Actualizando preview de metadata");
            
            // Delegar en el servicio
            Models.MetadataPreviewResult result = await _metadataService.ProcessPreviewAsync(content, FilePath);
            
            if (token.IsCancellationRequested)
                return;

            PreviewContent = result.PreviewContent;
            HasError = result.HasError;
            StatusMessage = HasError ? "Error en JSON" : "Preview actualizado correctamente.";
        }
        catch (TaskCanceledException)
        {
        }
    }
}
```

### Registro en DI

**Ubicación:** `src/Msi.TemplateCodeGenerator/DependencyInjection.cs`

```csharp
// Añadir al registro de servicios
services.AddSingleton<IMetadataService, MetadataService>();
```

## 3. Fix: CreateTestDocument — Documentar como artefacto de desarrollo

### Problema

`ProjectExplorerShellViewModel.CreateTestDocument` parece un artefacto de desarrollo/debug que genera un nombre de fichero aleatorio y abre un editor. No tiene uso legítimo en producción.

### Solución

**Opción A (recomendada):** Eliminar el método y su binding en la UI.

**Opción B:** Si tiene uso legítimo (ej. para testing manual), documentarlo como `#if DEBUG` y ocultarlo del menú en Release.

### Acción

Confirmar con el equipo si `CreateTestDocument` tiene uso legítimo. Si no, eliminarlo.

## 4. Testing

### Unit tests

- **MetadataService.ProcessPreviewAsync con JSON válido sin defaults**: Devuelve preview formateado sin merge.
- **MetadataService.ProcessPreviewAsync con JSON válido con defaults**: Devuelve preview con merge aplicado.
- **MetadataService.ProcessPreviewAsync con JSON inválido**: Devuelve preview con mensaje de error y `HasError = true`.
- **MetadataService.ProcessPreviewAsync con defaults inexistentes**: Devuelve preview sin merge, sin error.

### Integration tests

- **MetadataEditorShellViewModel**: Verificar que delega en `IMetadataService` y actualiza `PreviewContent` y `HasError`.
- **MetadataEditorShellViewModel**: Verificar que el debounce funciona correctamente.

## Referencias

- ADR: `docs/agents/proyecto/adr/ADR-001-command-routing.md`
- Especificación de Command Routing: `docs/agents/proyecto/especificaciones/command-routing.md`
- Especificación de Metadata Editor: `docs/agents/proyecto/especificaciones/metadata-editor.md`
- Guías de MVVM: `docs/agents/msi-guidelines-avalonia/msi-arquitectura-mvvm.md`
