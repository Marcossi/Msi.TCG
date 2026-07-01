# NavigationService con Docking para Avalonia

> Patron de navegacion para shells con docking IDE-like (Dock.Avalonia). Para shells con tabs o paneles simples, consultar `msi-navigation-simple.md`.

## Contexto

Este patron aplica a aplicaciones que necesitan:
- Paneles de herramientas fijos (tool panes)
- Documentos editables en pestanas (document panes)
- Layout redimensionable y potencialmente persistible
- Multiples vistas simultaneas (no solo una activa)

**Dependencia obligatoria**: Consultar `.agents/libraries-doc/Dock-*/index.md` antes de asumir comportamiento de Dock.Avalonia. No improvisar sobre la API.

## Estructura de archivos

```text
<App>/
├── Interfaces/
│   └── INavigationService.cs
├── Constants/
│   └── NavigationConstants.cs
└── UI/
    └── Services/
        └── Navigation/
            ├── NavigationService.cs
            └── AppDockFactory.cs
```

## Interfaz INavigationService (API rica)

```csharp
namespace MyApp.Interfaces;

public interface INavigationService
{
    IRootDock GetLayout();
    void ActivateDockable(string id);
    void HideDockable(string id);
    void OpenFile(string filePath);
    Task<bool> CloseDocumentAsync(string id);
    Task<bool> CanCloseAllAsync();
    IReadOnlyList<object> GetOpenEditors();
}
```

### Diferencias con el patron simple

| Patron simple | Patron docking |
|---|---|
| `NavigateTo<Vista>()` | `ActivateDockable(id)` |
| Una vista activa | Multiples vistas simultaneas |
| Sin documentos | Documentos editables con dirty state |
| Sin factory | `AppDockFactory` compone el layout |
| Singleton VMs | Scoped VMs para documentos |

## NavigationConstants

```csharp
namespace MyApp.Constants;

internal static class NavigationConstants
{
    public const string ProjectExplorerId = "ProjectExplorer";
    public const string TemplateEditorId = "TemplateEditor";
    public const string SettingsId = "Settings";
}
```

Todos los IDs de paneles se centralizan aqui. Prohibido hardcodear strings de ID en otros ficheros.

## AppDockFactory

Hereda de `Dock.Model.Mvvm.Factory`. Compone el layout de docking.

```csharp
using Dock.Model.Mvvm;

namespace MyApp.UI.Services.Navigation;

internal sealed class AppDockFactory : Factory
{
    private readonly IServiceProvider _serviceProvider;

    public AppDockFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override IRootDock CreateLayout()
    {
        // Composicion del layout: RootDock → ProportionalDock → ToolDock + DocumentDock
        // Los ViewModels se resuelven desde _serviceProvider
    }
}
```

### Reglas de AppDockFactory

- Recibe `IServiceProvider` como punto de composicion. Es la **unica excepcion** justificada al patron de no usar Service Locator.
- **No** recibe ViewModels directamente en el constructor (causaria dependencia circular).
- Resuelve ViewModels dentro de `CreateLayout()`, no en el constructor.
- Este patron **no** se traslada al resto de la aplicacion.

### Resolucion de dependencia circular

Problema:
```
MainShellViewModel → NavigationService → AppDockFactory → ProjectExplorerVM → NavigationService
```

Solucion: lazy initialization.
1. `NavigationService` NO inicializa el layout en su constructor.
2. La primera llamada a `GetLayout()` dispara `EnsureLayoutInitialized()`.
3. En ese momento, todos los singletons ya estan construidos.

## Layout de docking

```
RootDock
└── ProportionalDock (horizontal)
    ├── ToolDock (22% ancho) ← Paneles de herramientas
    ├── Splitter (redimensionable)
    └── DocumentDock (pestanas) ← Documentos editables
```

### Tool panes vs Document panes

| Tool pane | Document pane |
|---|---|
| Panel fijo, siempre visible | Pestana que se abre/cierra |
| Singleton (una instancia) | Scoped (una por pestana) |
| Ej: ProjectExplorer, Properties | Ej: TemplateEditor, Settings |

## DataTemplates genericos

En la vista de la shell, los DataTemplates mapean tipos de Dock a ViewModels:

```xml
<dock:DockControl.DataTemplates>
    <DataTemplate DataType="{x:Type dockMvvm:Tool}">
        <ContentControl Content="{Binding Context}" />
    </DataTemplate>
    <DataTemplate DataType="{x:Type dockMvvm:Document}">
        <ContentControl Content="{Binding Context}" />
    </DataTemplate>
</dock:DockControl.DataTemplates>
```

El `ViewLocator` resuelve automaticamente `ViewModel → View` por convencion.

## Scoped ViewModels para documentos

Los documentos editables (pestanas) usan lifetime **Scoped**: una instancia por pestana abierta.

```csharp
services.AddScoped<TemplateEditorShellViewModel>();
```

### Regla de resolucion Scoped

Un servicio Scoped **requiere** un `IServiceScope` explicito. Resolver desde el root provider sin crear scope es un error que convierte el Scoped en Singleton efectivo.

```csharp
// CORRECTO
using IServiceScope scope = _serviceProvider.CreateScope();
TemplateEditorShellViewModel editor = scope.ServiceProvider.GetRequiredService<TemplateEditorShellViewModel>();

// INCORRECTO — el Scoped se comporta como Singleton
TemplateEditorShellViewModel editor = _serviceProvider.GetRequiredService<TemplateEditorShellViewModel>();
```

## ICloseAware para documentos editables

Todo documento editable implementa `ICloseAware` para el flujo de cierre seguro:

```csharp
public interface ICloseAware
{
    Task<bool> CanCloseAsync();
}
```

Flujo de cierre:
1. Usuario cierra pestana o cierra la aplicacion.
2. `INavigationService.CloseDocumentAsync(id)` consulta `CanCloseAsync()`.
3. Si el documento tiene cambios sin guardar (dirty), muestra dialogo de confirmacion.
4. Si el usuario cancela, el cierre se aborta.
5. `CanCloseAllAsync()` itera todos los documentos abiertos antes de cerrar la aplicacion.

## Registro en IoC

```csharp
services.AddSingleton<AppDockFactory>();
services.AddSingleton<INavigationService, NavigationService>();

// Tool panes: Singleton
services.AddSingleton<ProjectExplorerShellViewModel>();

// Document panes: Scoped
services.AddScoped<TemplateEditorShellViewModel>();
```

## Flujo de apertura de documento

1. Usuario hace doble clic en un fichero del ProjectExplorer.
2. `ProjectExplorerShellViewModel` llama a `_navigationService.OpenFile(filePath)`.
3. `NavigationService.OpenFile()`:
   a. Crea un `IServiceScope`.
   b. Resuelve `TemplateEditorShellViewModel` desde el scope.
   c. Llama a `editor.LoadFileAsync(filePath)`.
   d. Crea un `Document` de Dock con el editor como `Context`.
   e. Anade el documento al `DocumentDock`.
   f. Activa la pestana.

## Antipatrones

- Inicializar el layout en el constructor de `NavigationService` (dependencia circular).
- Pasar ViewModels directamente al constructor de `AppDockFactory` (usar `IServiceProvider`).
- Resolver Scoped VMs desde el root provider sin crear scope.
- Hardcodear IDs de paneles fuera de `NavigationConstants`.
- Olvidar implementar `ICloseAware` en documentos editables (perdida de datos).
- No consultar la documentacion de Dock.Avalonia antes de asumir comportamiento de la API.
