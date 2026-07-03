# Arquitectura MVVM para Avalonia Desktop

> Capa reutilizable para aplicaciones de escritorio Avalonia basadas en MVVM.

## Regla de consulta de documentacion

> Usar el MCP de Avalonia (`mcp--avalonia-docs--search_avalonia_docs` y `mcp--avalonia-docs--lookup_avalonia_api`) con prioridad sobre busquedas web genericas.

Solo usar busquedas web genericas cuando:
- El MCP no devuelve resultados relevantes
- Se necesita informacion sobre paquetes de terceros no cubiertos por el MCP
- Se necesita informacion sobre breaking changes o versiones recientes

## Vocabulario

| Termino | Definicion | Ejemplo |
|---|---|---|
| **Screen** | Unidad de UI autocontenida: vista + ViewModel(s) + recursos locales | `ProjectExplorer`, `TemplateEditor`, `Settings` |
| **Feature** | Incremento funcional completo: rama + implementacion + integracion | "Anadir FileWatcher", "Implementar secciones recursivas" |

Una **Screen** es una carpeta en `UI/Views/`. Una **Feature** es un flujo de trabajo de desarrollo.

## Stack preferido

- Avalonia
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Logging y configuracion integrados con el host .NET

## Estructura del proyecto UI

```text
<App>/
├── App.axaml
├── App.axaml.cs
├── Program.cs
├── DependencyInjection.cs
├── ViewLocator.cs
├── Models/
├── Interfaces/
├── Services/
├── Constants/
├── Messages/
└── UI/
    ├── Views/
    │   └── <Screen>/
    │       ├── <Screen>View.axaml
    │       ├── <Screen>View.axaml.cs
    │       ├── ViewModels/
    │       │   ├── <Screen>ViewModel.cs
    │       │   └── (sub-VMs, DTOs de presentacion, clases base locales)
    │       └── Converters/
    │           └── (converters usados solo por esta screen)
    ├── Shared/
    │   ├── BaseViewModel.cs
    │   ├── Converters/
    │   │   └── (converters reutilizados por multiples screens)
    │   ├── Styles/
    │   │   └── (estilos globales de UI)
    │   └── Resources/
    │       └── (recursos compartidos: iconos, imagenes, etc.)
    └── Services/
        ├── Navigation/
        └── Dialogs/
```

### Regla de carpetas de Screen

Cada Screen tiene su carpeta dedicada en `UI/Views/<Screen>/`.

- **Dentro de la carpeta** van los ficheros de vista (`.axaml`, `.axaml.cs`).
- **`ViewModels/`** contiene el ViewModel principal de la screen y cualquier ViewModel auxiliar (sub-VMs, DTOs de presentacion, clases base locales).
- **`Converters/`** contiene converters usados exclusivamente por esta screen.
- **Archivos compartidos** (converters globales, estilos, recursos) van en `UI/Shared/`.

### Regla de namespaces

El namespace debe reflejar la ubicacion en carpetas:

```
UI/Views/<Screen>/                          → <App>.UI.Views.<Screen>
UI/Views/<Screen>/ViewModels/               → <App>.UI.Views.<Screen>.ViewModels
UI/Views/<Screen>/Converters/               → <App>.UI.Views.<Screen>.Converters
UI/Shared/                                  → <App>.UI.Shared
UI/Shared/Converters/                       → <App>.UI.Shared.Converters
UI/Services/Navigation/                     → <App>.UI.Services.Navigation
UI/Services/Dialogs/                        → <App>.UI.Services.Dialogs
```

### ViewLocator

El `ViewLocator` resuelve `ViewModel → View` por convencion. Con la estructura de carpetas propuesta, la convencion es:

```
<App>.UI.Views.<Screen>.ViewModels.<Name>ViewModel
    → <App>.UI.Views.<Screen>.<Name>View
```

Implementacion recomendada: switch explicito para screens conocidas + fallback por convencion para el resto.

## Reglas MVVM

- Los ViewModels exponen estado de binding y comandos.
- Los ViewModels no contienen logica de negocio compleja.
- La logica de negocio va en `Services/`.
- La logica dependiente de Avalonia va en `UI/Services/`.
- Los ViewModels heredan de un `BaseViewModel` comun.
- Usar `ObservableObject` como base funcional.
- Usar `[ObservableProperty]` y `[RelayCommand]` cuando encaje.

### Regla de capa de comandos (Command Routing)

Los comandos del Shell que operan en el documento/tool activo **deben** delegar en `ICommandRegistry`, no invocar servicios directamente.

**Flujo obligatorio para comandos contextuales:**
1. UI (menu/toolbar/keybinding) → `MainShellViewModel.SaveCommand`
2. `MainShellViewModel` → `ICommandRegistry.ExecuteAsync("Save")`
3. `CommandRegistry` → `ICommandContext.ActiveRoute` (el ViewModel del documento activo)
4. `ICommandRoute.ExecuteAsync("Save")` → ViewModel concreto (ej: `BaseTextEditorViewModel.SaveAsync()`)
5. ViewModel → Servicio de dominio (ej: `IFileService.WriteTextAsync()`)

**Excepcion:** Los comandos globales del Shell (abrir/cerrar proyecto, nuevo proyecto, salir) invocan servicios directamente, sin pasar por `ICommandRegistry`.

**Regla practica:**
- Si el comando opera sobre el documento activo (Save, Copy, Paste) → usar `ICommandRegistry`
- Si el comando opera sobre la aplicacion o proyecto (Open, Close, New, Exit) → invocar servicio directamente

Ver `especificaciones/command-routing.md` para detalles de implementacion.

### Regla de UI framework en ViewModel (CRITICA)

**Prohibido** acceder a tipos de Avalonia desde un ViewModel. Esto incluye:
- `Avalonia.Application.Current`
- `StorageProvider` (file pickers)
- `Window`, `TopLevel`
- Cualquier tipo del namespace `Avalonia.*`

Toda interaccion con el framework de UI se abstrae en un servicio de `UI/Services/`:
- File pickers → `IFileDialogService`
- Dialogos → `IDialogService`
- Navegacion → `INavigationService`

El ViewModel solo depende de interfaces, nunca de implementaciones de Avalonia.

### Regla de error handling en comandos

Todo `[RelayCommand]` async debe envolver su logica en try-catch:

```csharp
[RelayCommand]
private async Task OpenProjectAsync()
{
    _logger.LogInformation("[UI] Command: OpenProject");
    try
    {
        // logica del comando
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[UI] Error executing OpenProject");
        StatusMessage = $"Error: {ex.Message}";
    }
}
```

Prohibido dejar que excepciones de servicios propaguen sin manejar desde un comando.

### Regla de mensajeria con Unregister

Todo ViewModel que se suscriba a mensajes (`Register`) debe hacer `Unregister` al destruirse:

- Implementar `IDisposable` o `ICloseAware`.
- Llamar a `_messenger.Unregister<ThisViewModel>(this)` en el metodo de cleanup.
- Prohibido suscribirse sin desuscribirse (memory leak).

## Convencion de sufijos (Naming)

- **`<Feature>View`** → La vista/pantalla completa (`.axaml` + `.axaml.cs`).
- **`<Feature>ViewModel`** → La logica de presentacion y orquestacion de esa pantalla.
- **`<Entity>VM`** → Objeto adaptado (DTO de UI) que representa una unidad de datos o entidad de dominio. Lo usa el `ViewModel` para exponer colecciones o items individuales a la `View`.

Ejemplo:
```
TeachersView          → Pantalla de gestion de profesores
TeachersViewModel     → Logica de presentacion de esa pantalla
TeacherVM             → Objeto adaptado que representa un profesor en la UI
```

## Regla de servicios

- `Services/` para dominio o infraestructura reusable sin dependencias de UI.
- `UI/Services/` para navegacion, dialogos, docking, clipboard, shell, ventanas y otros elementos dependientes del framework.

### Regla de accesibilidad

- **Interfaces**: `public` (son el contrato).
- **Implementaciones de servicios**: `internal sealed` (detalle de implementacion).

```csharp
// Interfaz: public
public interface IProjectService { ... }

// Implementacion: internal sealed
internal sealed class ProjectService : IProjectService { ... }
```

Excepcion: clases que el framework necesita instanciar por reflexion (Views, ViewModels registrados en DI) pueden ser `public`.

## Audit Trail

Toda interaccion significativa del usuario produce una linea de log con nivel `Information` y prefijo `[UI]`.

### Que se loguea

| Evento | Formato |
|---|---|
| Ejecucion de comando | `[UI] Command: {CommandName}` |
| Navegacion | `[UI] Navigate: {Target}` |
| Apertura de documento | `[UI] Open document: {FilePath}` |
| Cierre de documento | `[UI] Close document: {DocumentId} (dirty={bool})` |
| FileDialog — seleccion | `[UI] FileDialog: Selected '{FilePath}'` |
| FileDialog — cancelacion | `[UI] FileDialog: Cancelled` |
| Dialogo de confirmacion | `[UI] Dialog: '{Question}' → {Result}` |
| Operacion de proyecto | `[UI] Project: {Operation} '{ProjectPath}'` |

### Que NO se loguea

- Pulsaciones de tecla individuales
- Cambios de foco
- Scroll, hover, resize
- Debounce interno (no es accion de usuario)
- Operaciones triviales de UI

### Donde se loguea

- **En el ViewModel**: primera linea del `[RelayCommand]`.
- **En `IDialogService`**: cuando se muestra el dialogo y cuando se recibe la respuesta.
- **En `IFileDialogService`**: cuando el usuario selecciona o cancela.

### Formato en Serilog

```
[HH:mm:ss INF] [UI] Command: OpenProject
[HH:mm:ss INF] [UI] FileDialog: Selected 'C:\Projects\MyApp.scribanproj'
[HH:mm:ss INF] [UI] Project: Open 'C:\Projects\MyApp.scribanproj'
```

El objetivo es poder reproducir el flujo del usuario leyendo el log.

## Logging

### Regla de ILogger<T>

Todo servicio y ViewModel con logica no trivial debe inyectar `ILogger<T>` en su constructor:

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

El logging no es opcional. Un servicio sin logging es un servicio que no se puede diagnosticar en produccion.

### Regla de catch con logging

Todo `catch` debe loguear la excepcion. Prohibido `catch {}` vacio o `catch (Exception) {}` sin logging:

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
catch (Exception ex) { _ = ex; }
```

## Antipatrones

### Antipatrones estructurales

- **Poner validacion de dominio en un ViewModel.** La logica de negocio va en `Services/`.
- **Acoplar un ViewModel a otro ViewModel concreto.** Usar mensajeria o servicio compartido.
- **Resolver servicios desde el contenedor dentro del ViewModel** salvo en infraestructura muy localizada y justificada (`AppDockFactory`).
- **Shell invocando servicios directamente para comandos contextuales.** Los comandos que operan sobre el documento activo (Save, Copy, Paste) deben usar `ICommandRegistry`, no invocar `IFileService` o similar directamente desde el Shell. Esto rompe el desacoplamiento entre Shell y documentos.

### Antipatrones de UI

- **UI framework en ViewModel.** Acceder a `Avalonia.Application.Current`, `StorageProvider`, `Window` desde un VM. Toda interaccion con Avalonia se abstrae en `UI/Services/`.
- **File pickers sin abstraer.** Llamar a `SaveFilePickerAsync()` o `OpenFilePickerAsync()` directamente desde un VM. Deben estar en `IFileDialogService`.
- **ViewModel sin error handling.** Comandos `[RelayCommand]` que llaman a servicios sin try-catch. Todo comando async debe manejar excepciones y loguearlas.

### Antipatrones de infraestructura

- **Sin unregister en mensajeria.** `Register()` sin `Unregister()`. Todo VM suscrito a mensajes debe implementar cleanup.
- **Catch vacio.** `catch {}` o `catch (Exception) {}` sin logging. Todo catch debe loguear la excepcion.
- **Logging ausente.** Servicios y VMs sin `ILogger<T>`. Todo servicio y VM con logica no trivial inyecta logging.
- **Implementaciones publicas.** Servicios con `public class` cuando deben ser `internal sealed`. Solo las interfaces son `public`.
- **Scoped desde root provider.** Resolver un servicio Scoped sin crear `IServiceScope` explicito. Convierte el Scoped en Singleton efectivo.
