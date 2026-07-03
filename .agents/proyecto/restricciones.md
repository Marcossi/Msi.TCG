# Restricciones y conveniencias

> Reglas, restricciones y conveniencias que deben cumplirse al trabajar con este proyecto.

## Restricciones de implementación

### Scriban: Solo métodos estáticos

En Scriban, solo se pueden registrar métodos estáticos. Los métodos de instancia no son accesibles desde el `ScriptObject`.

Cuando se necesiten métodos disponibles en plantillas:
- Deben ser estáticos, o
- Registrarse como delegados `Func<>` en el `ScriptObject`

Ejemplo correcto:
```csharp
public static string FormatName(string name) => name.ToUpper();
scriptObject.AddStatic("FormatName", FormatName);
```

Ejemplo incorrecto:
```csharp
// Este método NO será accesible desde Scriban
public string FormatName(string name) => name.ToUpper();
```

### Async/I/O: Todos los métodos con I/O deben ser Async

Obligatorios:
- Apertura/cierre de proyectos (`OpenProjectAsync`, `CloseProjectAsync`)
- Guardado (`SaveProjectAsync`)
- Renderizado de plantillas (`ProcessTemplateAsync`)

Razón: Mantener la UI responsive, permitir cleanup con I/O, y consistencia.

### Accesibilidad: Implementaciones internal sealed

- **Interfaces**: `public` (son el contrato).
- **Implementaciones de servicios**: `internal sealed` (detalle de implementación).

```csharp
// Interfaz: public
public interface IProjectService { ... }

// Implementación: internal sealed
internal sealed class ProjectService : IProjectService { ... }
```

Excepción: clases que el framework necesita instanciar por reflexión (Views, ViewModels registrados en DI) pueden ser `public`.

### Logging obligatorio

Todo servicio y ViewModel con lógica no trivial debe inyectar `ILogger<T>` en su constructor. El logging no es opcional.

```csharp
internal sealed class ProjectService : IProjectService
{
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }
}
```

Un servicio sin logging es un servicio que no se puede diagnosticar en producción.

### Error handling: Prohibido catch vacío

Todo `catch` debe loguear la excepción. Prohibido `catch {}` vacío o `catch (Exception) {}` sin logging:

```csharp
// CORRECTO
catch (Exception ex)
{
    _logger.LogError(ex, "Error al procesar plantilla");
    return TemplateResult.Failure(ex.Message);
}

// INCORRECTO — prohibido
catch { }
catch (Exception) { }
```

### Error handling en comandos

Todo `[RelayCommand]` async debe envolver su lógica en try-catch:

```csharp
[RelayCommand]
private async Task OpenProjectAsync()
{
    _logger.LogInformation("[UI] Command: OpenProject");
    try
    {
        // lógica del comando
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[UI] Error executing OpenProject");
        StatusMessage = $"Error: {ex.Message}";
    }
}
```

Prohibido dejar que excepciones de servicios propaguen sin manejar desde un comando.

## Conveniencias de código

### Nomenclatura

- Clases → PascalCase (`ProjectService`)
- Interfaces → PascalCase + `I` (`IProjectService`)
- Métodos → PascalCase (`OpenProjectAsync()`)
- Propiedades → PascalCase (`ProjectName`)
- Variables/parámetros → camelCase (`projectName`)
- Campos privados → `_camelCase` (`_projectService`)
- Constantes → PascalCase (`MaxRetries`)

### Idioma

- **Código**: Inglés (nombres de variables, clases, métodos).
- **Comentarios/XML docs**: Español (Castellano).

### Estilo de código

- **`var` deshabilitado**: Usar siempre tipos explícitos. El `.editorconfig` lo fuerza.
- **File-scoped namespaces**: Obligatorios. El `.editorconfig` lo fuerza como error.
- **Usings redundantes**: Eliminar `using System;`, `using System.IO;`, `using System.Linq;` cuando están cubiertos por `ImplicitUsings`.

### Terminología

Se usa **"Project"** en lugar de "Workspace" para alinearse con la experiencia de desarrolladores C# (similar a Visual Studio).

### BaseViewModel

Todos los ViewModels heredan de `BaseViewModel`, que a su vez hereda de `ObservableObject` de CommunityToolkit.Mvvm. Punto central para lógica común de ViewModels.

## Restricciones de arquitectura

### Capa de comandos (Command Routing)

Los comandos del Shell se dividen en dos categorías:

1. **Comandos globales**: Operan sobre la aplicación o el proyecto (abrir, cerrar, nuevo proyecto, salir). Invocan servicios directamente.
2. **Comandos contextuales**: Operan sobre el documento/tool activo (guardar archivo, copiar, pegar). **Deben** usar `ICommandRegistry` para resolver el comando en el ViewModel activo.

**Regla:** Prohibido que el Shell invoque servicios de dominio directamente para comandos contextuales. El flujo obligatorio es:

```
Shell → ICommandRegistry → ICommandContext.ActiveRoute → ICommandRoute.ExecuteAsync() → Servicio
```

**Ejemplo correcto (Save contextual):**
```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync()
{
    _logger.LogInformation("[UI] Command: Save (contextual)");
    await _commandRegistry.ExecuteAsync("Save");
}
```

**Ejemplo incorrecto (Shell acoplado al editor):**
```csharp
[RelayCommand]
private async Task SaveAsync()
{
    // INCORRECTO: El Shell no debe conocer IFileService ni el editor activo
    await _fileService.WriteTextAsync(editorVm.FilePath, editorVm.Content);
}
```

**Excepción:** Los comandos globales del Shell pueden invocar servicios directamente:
```csharp
[RelayCommand]
private async Task OpenProjectAsync()
{
    // CORRECTO: OpenProject es un comando global, no contextual
    await _projectService.OpenProjectAsync(path);
}
```

Ver `especificaciones/command-routing.md` para detalles de implementación.

### Separación de Contexto vs Servicio

- **`IProjectContext`** (Estado):
  - Solo propiedades de lectura pública.
  - Representa el "estado actual" de la aplicación respecto al proyecto.
  - Los ViewModels **leen** de aquí para mostrar información.
  - **No tiene métodos** de modificación expuestos públicamente.

- **`IProjectService`** (Operaciones):
  - Métodos que modifican el contexto indirectamente.
  - Contiene toda la lógica compleja (carga XML, validaciones, FileWatcher).
  - Los ViewModels **invocan** este servicio para ejecutar acciones.

### Mensajería vs Llamadas Directas

Los ViewModels no se comunican directamente entre sí. Usan el sistema de mensajería (`WeakReferenceMessenger`) o servicios compartidos.

**Regla de Unregister**: Todo ViewModel que se suscriba a mensajes debe hacer `Unregister` al destruirse. Implementar `IDisposable` o `ICloseAware` y llamar a `_messenger.Unregister<ThisViewModel>(this)` en el método de cleanup. Prohibido suscribirse sin desuscribirse (memory leak).

## Audit Trail

Toda interacción significativa del usuario produce una línea de log con nivel `Information` y prefijo `[UI]`.

### Eventos logueados

| Evento | Formato |
|---|---|
| Ejecución de comando | `[UI] Command: {CommandName}` |
| Navegación | `[UI] Navigate: {Target}` |
| Apertura de documento | `[UI] Open document: {FilePath}` |
| Cierre de documento | `[UI] Close document: {DocumentId} (dirty={bool})` |
| FileDialog — selección | `[UI] FileDialog: Selected '{FilePath}'` |
| FileDialog — cancelación | `[UI] FileDialog: Cancelled` |
| Diálogo de confirmación | `[UI] Dialog: '{Question}' → {Result}` |
| Operación de proyecto | `[UI] Project: {Operation} '{ProjectPath}'` |

### Eventos NO logueados

- Pulsaciones de tecla individuales
- Cambios de foco
- Scroll, hover, resize
- Debounce interno (no es acción de usuario)
- Operaciones triviales de UI

### Dónde se loguea

- **En el ViewModel**: primera línea del `[RelayCommand]`.
- **En `IDialogService`**: cuando se muestra el diálogo y cuando se recibe la respuesta.
- **En `IFileDialogService`**: cuando el usuario selecciona o cancela.

### Objetivo

Poder reproducir el flujo del usuario leyendo el log. El audit trail permite diagnosticar problemas reportados por usuarios sin necesidad de reproducirlos manualmente.

## Serialización de proyectos

- **Formato**: JSON con soporte JSONC (comentarios leídos pero no preservados al guardar).
- **Abstracción**: `IProjectSerializer` permite cambiar de formato fácilmente.
- **Implementación**: `JsonProjectSerializer` usa `System.Text.Json` con `ReadCommentHandling.Skip`.
- **Extensión**: `.scribanproj`
- **Estructura**: JSON con `fileFormatVersion` y `project`.

Ejemplo:
```json
{
  "fileFormatVersion": 1,
  "project": {
    "name": "NuevoProyecto"
  }
}
```

Futuras mejoras: Migrar a JSON5 si se requiere preservar comentarios al guardar.

## Estructura de carpetas del proyecto de usuario

Similar a proyectos de Visual Studio:
- **Archivo principal**: `*.scribanproj` (JSON con metadatos del proyecto).
- **Carpeta del proyecto**: Contiene el `.scribanproj` y todos los archivos/subcarpetas.
- **FileWatcher**: Vigila cambios en la carpeta (añadir/eliminar archivos) para actualizar el modelo en memoria.
- **Plantillas**: Archivos `.scriban` dentro de la carpeta del proyecto.
