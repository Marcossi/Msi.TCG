# MainShellView

> Descripción detallada de MainShellView. Shell principal de la aplicación. Contiene el sistema de navegación con paneles dockeables (Dock.Avalonia) y aloja todas las demás pantallas. Se muestra dentro de `MainWindow`.

## Ubicación

- **Carpeta**: `UI/Views/`
- **Ficheros**:
  - `MainShellView.axaml` → Vista XAML
  - `MainShellView.axaml.cs` → Code-behind
  - `MainShellViewModel.cs` → ViewModel

## Estructura del layout

El layout se gestiona mediante `Dock.Avalonia` a través de `INavigationService`:

```
RootDock
└── ProportionalDock (horizontal)
    ├── ToolDock (22% ancho) ← ProjectExplorer
    ├── Splitter (redimensionable)
    └── DocumentDock (pestañas) ← Editores, Settings
```

## MainShellViewModel

Propiedades:
- `Layout` → `IRootDock` devuelto por `INavigationService.GetLayout()`

Dependencias:
- `INavigationService` → Gestión de paneles dockeables
- `IProjectService` → Operaciones de proyecto

Comandos:
- `NewProjectAsync()` → Crea un nuevo proyecto
- `OpenProjectAsync()` → Abre un proyecto existente
- `CloseProjectAsync()` → Cierra el proyecto actual
- `SaveProjectAsync()` → Guarda el proyecto actual
- `SaveProjectAsAsync()` → Guarda el proyecto en nueva ubicación
- `Exit()` → Sale de la aplicación

Binding en XAML:
```xml
<dock:DockControl>
    <dock:DockControl.Layout>
        <Binding Path="Layout" />
    </dock:DockControl.Layout>
</dock:DockControl>
```

## DataTemplates para Dock

En `MainShellView.axaml`, los DataTemplates genéricos mapean tipos de Dock a ViewModels:

```xml
<dock:DockControl.DataTemplates>
    <!-- Tool → ViewLocator resuelve la View desde el Context (ViewModel) -->
    <DataTemplate DataType="{x:Type dockMvvm:Tool}">
        <ContentControl Content="{Binding Context}" />
    </DataTemplate>
    <!-- Document → ViewLocator resuelve la View desde el Context -->
    <DataTemplate DataType="{x:Type dockMvvm:Document}">
        <ContentControl Content="{Binding Context}" />
    </DataTemplate>
</dock:DockControl.DataTemplates>
```

El `ViewLocator` (en `App.axaml`) resuelve automáticamente `ViewModel → View` por convención de nombres.

## Resolución de dependencias circulares

Problema original:
```
MainShellViewModel → NavigationService → AppDockFactory → ProjectExplorerVM → NavigationService ❌
```

Solución implementada:
1. `AppDockFactory` recibe `IServiceProvider`, no los ViewModels directamente.
2. `NavigationService` NO inicializa el layout en su constructor.
3. La primera llamada a `GetLayout()` dispara `EnsureLayoutInitialized()`.
4. **Ahora** sí se resuelven los ViewModels (todos ya construidos). ✅

## Pantallas alojadas

- ProjectExplorer → Tool → ID: `ProjectExplorerId`
- TemplateEditor → Document → ID: `TemplateEditorId`
- Settings → Document → ID: `SettingsId`

## Registro en DI

- `MainWindow` → `MainWindow` → Singleton
- `MainShellViewModel` → `MainShellViewModel` → Singleton
