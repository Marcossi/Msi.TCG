# Plan de Implementación: Editor de Metadata y Unificación de Apertura de Archivos

> Fecha: 2026-07-03
> Estado: Planificado
> Referencia: ADR-003 (pendiente de actualización)

## Contexto

Actualmente el `ProjectExplorer` solo abre archivos `.scriban` mediante single-click en `OnSelectedFileEntryChanged`. Esto es inconsistente y no escala para nuevos tipos de editores.

**Objetivo:** Unificar la apertura de archivos para que cualquier tipo de archivo editable se abra con doble-click mediante un comando genérico que delegue en `NavigationService`.

## Decisiones Arquitectónicas

### 1. ProjectExplorer es agnóstico al tipo de archivo

- **No** debe conocer los tipos de editores.
- Solo captura la intención del usuario (doble-click) y delega en `NavigationService`.
- Un único comando `OpenFileCommand` para todos los tipos de archivos.

### 2. Comportamiento uniforme

- **Single-click**: Seleccionar el item (resaltado visual). No abre archivos.
- **Doble-click**: Abrir el item en un editor. Aplica a todos los tipos editables.

### 3. Detección de editor por extensión

`NavigationService.ResolveEditor()` decide qué editor usar según la extensión del archivo.

## Plan de Implementación

### Fase 1: Clasificación de archivos

#### Tarea 1.1: Extender FileType

**Archivo:** `src/Msi.TemplateCodeGenerator/Models/FileType.cs`

```csharp
public enum FileType
{
    Project,
    Script,
    Metadata,  // NUEVO: JSON en metadata/
    Directory,
    Other
}
```

#### Tarea 1.2: Clasificar JSON en metadata/

**Archivo:** `src/Msi.TemplateCodeGenerator/Services/Project/ProjectService.Files.cs`

Modificar `GetFileType()` para detectar JSON en carpeta `metadata/`:

```csharp
private static FileType GetFileType(string path)
{
    if (Directory.Exists(path))
        return FileType.Directory;
    
    if (path.EndsWith(".scriban", StringComparison.OrdinalIgnoreCase))
        return FileType.Script;
    
    if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && 
        IsInMetadataFolder(path))
        return FileType.Metadata;
    
    return FileType.Other;
}

private static bool IsInMetadataFolder(string path)
{
    string normalized = path.Replace('\\', Path.DirectorySeparatorChar);
    return normalized.Contains($"{Path.DirectorySeparatorChar}metadata{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
}
```

#### Tarea 1.3: Actualizar converters

**Archivos:**
- `src/Msi.TemplateCodeGenerator/UI/Views/ProjectExplorer/Converters/FileTypeToIconConverter.cs`
- `src/Msi.TemplateCodeGenerator/UI/Views/ProjectExplorer/Converters/FileTypeToForegroundConverter.cs`

Añadir icono y color para `FileType.Metadata`:
- Icono: `\uE8D6` (Database) o similar
- Color: `DarkSlateBlue` o similar

### Fase 2: Unificación del comando de apertura

#### Tarea 2.1: Eliminar lógica de apertura de OnSelectedFileEntryChanged

**Archivo:** `src/Msi.TemplateCodeGenerator/UI/Views/ProjectExplorer/ViewModels/ProjectExplorerShellViewModel.cs`

**Antes:**
```csharp
partial void OnSelectedFileEntryChanged(object? value)
{
    if (value is FileEntryViewModel entry && entry.Type == FileType.Script)
    {
        string absolutePath = Path.Combine(...);
        _navigationService.OpenFile(absolutePath);
    }
}
```

**Después:**
```csharp
partial void OnSelectedFileEntryChanged(object? value)
{
    if (value is FileEntryViewModel entry)
    {
        _logger.LogInformation("[UI] FileEntry seleccionado: '{Name}' (Type={Type})", 
            entry.Name, entry.Type);
    }
    // ELIMINAR: Lógica de apertura de archivos
}
```

#### Tarea 2.2: Añadir comando OpenFileCommand genérico

**Archivo:** `src/Msi.TemplateCodeGenerator/UI/Views/ProjectExplorer/ViewModels/ProjectExplorerShellViewModel.cs`

```csharp
[RelayCommand]
private async Task OpenFile(FileEntryViewModel? entry)
{
    if (entry == null)
    {
        _logger.LogDebug("OpenFile invocado con entry null");
        return;
    }
    
    _logger.LogInformation("[UI] Command: OpenFile '{Name}' (Type={Type}, Path={Path})", 
        entry.Name, entry.Type, entry.RelativePath);
    
    // Solo abrir archivos que no sean directorios ni el proyecto raíz
    if (entry.Type == FileType.Directory || entry.Type == FileType.Project)
    {
        _logger.LogDebug("Item no editable ignorado: '{Name}' (Type={Type})", entry.Name, entry.Type);
        return;
    }
    
    string absolutePath = Path.Combine(
        _projectContext.CurrentProject!.FolderPath,
        entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
    
    _logger.LogDebug("Invocando NavigationService.OpenFile('{AbsolutePath}')", absolutePath);
    await _navigationService.OpenFile(absolutePath);
    _logger.LogInformation("[UI] Archivo abierto en editor: '{Path}'", entry.RelativePath);
}
```

### Fase 3: Handler de doble-click en la Vista

#### Tarea 3.1: Añadir evento DoubleTapped en el TreeView

**Archivo:** `src/Msi.TemplateCodeGenerator/UI/Views/ProjectExplorer/ProjectExplorerShellView.axaml`

Modificar el `TreeView` para añadir el handler:

```xml
<TreeView x:Name="FileTree" 
          ItemsSource="{Binding FileTree}" 
          SelectedItem="{Binding SelectedFileEntry}"
          DoubleTapped="OnTreeViewDoubleTapped">
    <!-- ... resto del contenido ... -->
</TreeView>
```

#### Tarea 3.2: Implementar handler en el code-behind

**Archivo:** `src/Msi.TemplateCodeGenerator/UI/Views/ProjectExplorer/ProjectExplorerShellView.axaml.cs`

```csharp
private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
{
    // Obtener el TreeViewItem bajo el cursor
    if (e.Source is StyledElement element)
    {
        TreeViewItem? treeViewItem = null;
        Visual? current = element;
        
        while (current != null)
        {
            if (current is TreeViewItem item)
            {
                treeViewItem = item;
                break;
            }
            current = current.VisualParent as Visual;
        }
        
        if (treeViewItem?.DataContext is FileEntryViewModel entry && 
            DataContext is ProjectExplorerShellViewModel vm)
        {
            vm.OpenFileCommand.Execute(entry);
            e.Handled = true;
        }
    }
}
```

**Nota:** Necesario añadir `using Avalonia.Input;` y `using Avalonia.VisualTree;` en el code-behind.

### Fase 4: Mejorar logging en NavigationService

#### Tarea 4.1: Añadir logging en OpenFile

**Archivo:** `src/Msi.TemplateCodeGenerator/UI/Services/Navigation/NavigationService.cs`

Modificar `OpenFile()` para añadir logging:

```csharp
public async Task OpenFile(string filePath)
{
    _logger.LogInformation("[UI] NavigationService: OpenFile '{FilePath}'", filePath);
    
    if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path cannot be empty.", nameof(filePath));

    // Buscar si ya está abierto
    IEnumerable<IDockable> existingDocs = _factory.Find(d => d.Id == $"File_{filePath}");
    IDockable? existingDoc = existingDocs.FirstOrDefault();
    if (existingDoc != null)
    {
        _logger.LogDebug("Archivo ya abierto, activando '{FilePath}'", filePath);
        _factory.SetActiveDockable(existingDoc);
        OnActiveDockableChanged(existingDoc);
        return;
    }

    // Crear scope explícito para resolver el ViewModel Scoped
    IServiceScope scope = _serviceProvider.CreateScope();
    BaseViewModel editorVM = ResolveEditor(scope, filePath);
    _logger.LogDebug("Editor resuelto: {EditorType} para '{FilePath}'", editorVM.GetType().Name, filePath);
    
    await LoadEditorFileAsync(editorVM, filePath);

    Document document = new()
    {
        Id = $"File_{filePath}",
        Title = Path.GetFileName(filePath),
        Context = editorVM,
        CanClose = true
    };

    // Almacenar el scope para disposal posterior al cerrar
    _documentScopes[document.Id] = scope;

    // Buscar el DocumentDock y añadir el documento
    IDocumentDock? documentDock = FindById(NavigationConstants.DocumentsPaneId) as IDocumentDock;
    if (documentDock != null)
    {
        _factory.AddDockable(documentDock, document);
        _factory.SetActiveDockable(document);
        OnActiveDockableChanged(document);
    }

    _logger.LogInformation("[UI] Archivo abierto en editor: '{FilePath}' (Editor={EditorType})", 
        filePath, editorVM.GetType().Name);
}
```

#### Tarea 4.2: Añadir logging en ResolveEditor

**Archivo:** `src/Msi.TemplateCodeGenerator/UI/Services/Navigation/NavigationService.cs`

```csharp
private BaseViewModel ResolveEditor(IServiceScope scope, string filePath)
{
    string extension = Path.GetExtension(filePath).ToLowerInvariant();

    BaseViewModel editor = extension switch
    {
        ".json" when IsMetadataFile(filePath)
            => scope.ServiceProvider.GetRequiredService<MetadataEditorShellViewModel>(),
        _ => scope.ServiceProvider.GetRequiredService<TemplateEditorShellViewModel>()
    };
    
    _logger.LogDebug("Editor resuelto para extensión '{Extension}': {EditorType}", 
        extension, editor.GetType().Name);
    
    return editor;
}
```

## Resumen de cambios

| # | Archivo | Cambio |
|---|---------|--------|
| 1 | `Models/FileType.cs` | Añadir `FileType.Metadata` |
| 2 | `Services/Project/ProjectService.Files.cs` | Clasificar JSON en `metadata/` como `Metadata` |
| 3 | `UI/Views/ProjectExplorer/Converters/FileTypeToIconConverter.cs` | Añadir icono para `Metadata` |
| 4 | `UI/Views/ProjectExplorer/Converters/FileTypeToForegroundConverter.cs` | Añadir color para `Metadata` |
| 5 | `UI/Views/ProjectExplorer/ViewModels/ProjectExplorerShellViewModel.cs` | Eliminar lógica de apertura de `OnSelectedFileEntryChanged`, añadir logging |
| 6 | `UI/Views/ProjectExplorer/ViewModels/ProjectExplorerShellViewModel.cs` | Añadir comando `OpenFileCommand` genérico con logging |
| 7 | `UI/Views/ProjectExplorer/ProjectExplorerShellView.axaml` | Añadir handler `DoubleTapped="OnTreeViewDoubleTapped"` |
| 8 | `UI/Views/ProjectExplorer/ProjectExplorerShellView.axaml.cs` | Implementar `OnTreeViewDoubleTapped` |
| 9 | `UI/Services/Navigation/NavigationService.cs` | Añadir logging en `OpenFile` y `ResolveEditor` |

## Flujo completo con logging

```
1. Usuario hace doble-click en Workflow.json
   ↓
2. [UI] FileEntry seleccionado: 'Workflow.json' (Type=Metadata)  ← OnSelectedFileEntryChanged
   ↓
3. [UI] Command: OpenFile 'Workflow.json' (Type=Metadata, Path=metadata/elements/Workflow.json)  ← OpenFileCommand
   ↓
4. Invocando NavigationService.OpenFile('...\metadata\elements\Workflow.json')
   ↓
5. [UI] NavigationService: OpenFile '...\metadata\elements\Workflow.json'  ← NavigationService.OpenFile
   ↓
6. Editor resuelto para extensión '.json': MetadataEditorShellViewModel  ← ResolveEditor
   ↓
7. Editor resuelto: MetadataEditorShellViewModel para '...\metadata\elements\Workflow.json'
   ↓
8. [UI] Editor: Cargando '...\metadata\elements\Workflow.json'  ← BaseTextEditorViewModel.LoadFileAsync
   ↓
9. [UI] Archivo abierto en editor: '...\metadata\elements\Workflow.json' (Editor=MetadataEditorShellViewModel)
   ↓
10. Preview de metadata actualizado correctamente  ← MetadataEditorShellViewModel.UpdatePreview
```

## Criterios de aceptación

- [ ] Doble-click en `.scriban` abre `TemplateEditorShellView`
- [ ] Doble-click en `.json` en `metadata/` abre `MetadataEditorShellView`
- [ ] Single-click solo selecciona, no abre archivos
- [ ] Doble-click en directorios o proyecto raíz no hace nada
- [ ] Logging completo en todas las capas del flujo
- [ ] Build sin errores
- [ ] Tests existentes pasan (43/43)

## Notas para el implementador

1. **Orden de implementación**: Seguir las fases en orden (1 → 2 → 3 → 4).
2. **Testing manual**: Probar con el proyecto de ejemplo `resources/ProjectSample1_Basic/`.
3. **Logging**: Verificar que el log muestra el flujo completo al hacer doble-click.
4. **Convenciones**: Seguir las convenciones del proyecto (var deshabilitado, file-scoped namespaces, etc.).
