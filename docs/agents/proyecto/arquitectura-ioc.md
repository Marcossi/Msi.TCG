# Arquitectura de contenedores IoC

> Descripción de la arquitectura de inyección de dependencias del proyecto.

## Estructura del proyecto

El proyecto es una **aplicación de escritorio única** con toda la lógica en un solo proyecto:

- **Sin capas de abstracción innecesarias**: Scriban se usa directamente, no hay capa "Core" intermedia.
- **Servicios en `Services/`**: Lógica de negocio sin dependencias de UI.
- **Servicios de UI en `UI/Services/`**: Lógica que depende de Avalonia o Dock.Avalonia.

## Principio fundamental

> **Regla de oro**: Los servicios son operaciones (sin estado por proyecto). El contexto es estado (sin operaciones).

- **Servicios** → IoC gestiona su ciclo de vida (Singleton o Scoped)
- **Contexto** → Objeto que representa el estado actual, pasado por referencia

## Jerarquía de contenedores

### Nivel 1: Root Provider (Singleton - IoC puro)

Servicios sin estado que viven toda la aplicación:
- `IProjectService`
- `ITemplatesService`
- `IFileService`
- `INavigationService`
- `IDialogService`
- `IMessenger`
- `IProjectContext`

Lifetime: Singleton. Ciclo de vida: toda la aplicación.

### Nivel 2: IProjectContext (Interfaz de lectura)

Interfaz de SOLO LECTURA expuesta a los ViewModels:
- `Project? CurrentProject`
- `bool IsProjectOpen`
- `string? CurrentProjectPath`

Los ViewModels LEEN de aquí, NO modifican. Solo `ProjectService` puede modificar el contexto subyacente.

Lifetime: Singleton (apunta al proyecto actual).

### Nivel 3: ProjectContext (Implementación)

Contenedor de estado del proyecto activo:
- `Project Project` ← Entidad de dominio
- `string? CurrentProjectPath` ← Ruta del proyecto
- `bool IsProjectOpen` ← Derivado de `Project != null`

NO tiene métodos de operaciones. Solo estado. Se actualiza al abrir/cerrar/guardar proyecto.

```csharp
internal sealed class ProjectContext : IProjectContext
{
    public Project? CurrentProject { get; internal set; }
    public string? CurrentProjectPath { get; internal set; }
    public bool IsProjectOpen => CurrentProject != null;
}
```

## Patrón MVVM + Contexto

Servicios inyectados por constructor, contexto inyectado por constructor (solo lectura):

```csharp
public class TemplateEditorShellViewModel : BaseViewModel
{
    private readonly IProjectContext _projectContext;   // Contexto (solo lectura)
    private readonly ITemplatesService _templateService; // Servicio (IoC)
    private readonly IFileService _fileService;          // Servicio (IoC)

    public TemplateEditorShellViewModel(
        IProjectContext projectContext,
        ITemplatesService templateService,
        IFileService fileService)
    {
        _projectContext = projectContext;
        _templateService = templateService;
        _fileService = fileService;
    }
}
```

Regla de separación: Los servicios se inyectan, el contexto se inyecta (pero solo se lee).

## Flujo de trabajo

### Abrir proyecto (comando global)

1. Usuario hace clic en "Abrir Proyecto"
2. `MainShellViewModel.OpenProjectAsync(path)`
3. `ProjectService.OpenProjectAsync(path)`:
   a. Deserializa Project desde disco (`IProjectSerializer`)
   b. Actualiza `_context.CurrentProject = project`
   c. Actualiza `_context.CurrentProjectPath = projectPath`
   d. Envía `ProjectOpenedMessage`
4. ViewModels reciben mensaje y refrescan UI

**Nota:** Los comandos globales del Shell (abrir, cerrar, nuevo proyecto) invocan servicios directamente, sin pasar por `ICommandRegistry`.

### Cerrar proyecto (comando global)

1. Usuario hace clic en "Cerrar Proyecto"
2. `MainShellViewModel.CloseProjectAsync()`
3. `ProjectService.CloseProjectAsync()`:
   a. `_context.CurrentProject = null`
   b. `_context.CurrentProjectPath = null`
   c. Envía `ProjectClosedMessage`
4. ViewModels reciben mensaje y limpian UI

### Guardar archivo (comando contextual)

1. Usuario presiona Ctrl+S o hace clic en "Guardar"
2. `MainShellViewModel.SaveCommand` → `ICommandRegistry.ExecuteAsync("Save")`
3. `CommandRegistry` consulta `ICommandContext.ActiveRoute`
4. Si hay un editor activo (`BaseTextEditorViewModel`):
   a. `ActiveRoute.CanExecute("Save")` → true (si `IsDirty && FilePath != ""`)
   b. `ActiveRoute.ExecuteAsync("Save")` → `BaseTextEditorViewModel.SaveAsync()`
   c. `SaveAsync()` → `IFileService.WriteTextAsync()`
5. Si no hay editor activo:
   a. `ActiveRoute` es null → `CanExecute` devuelve false
   b. El comando no se ejecuta (botón deshabilitado)

**Regla:** Los comandos contextuales (Save, Copy, Paste, etc.) **deben** usar `ICommandRegistry`. Los comandos globales (OpenProject, CloseProject, etc.) invocan servicios directamente.

## Registro de servicios (DI)

Fichero: `DependencyInjection.cs`

- `MainWindow` → `MainWindow` → Singleton
- `MainShellViewModel` → `MainShellViewModel` → Singleton
- `SettingsShellViewModel` → `SettingsShellViewModel` → Singleton
- `AppDockFactory` → `AppDockFactory` → Singleton
- `NavigationService` → `NavigationService` → Singleton (implementa `INavigationService` e `ICommandContext`)
- `INavigationService` → `NavigationService` → Singleton
- `ICommandContext` → `NavigationService` → Singleton
- `ICommandRegistry` → `CommandRegistry` → Singleton
- `ProjectExplorerShellViewModel` → `ProjectExplorerShellViewModel` → Singleton
- `TemplateEditorShellViewModel` → `TemplateEditorShellViewModel` → **Scoped**
- `IMessenger` → `WeakReferenceMessenger.Default` → Singleton
- `IProjectContext` → `ProjectContext` → Singleton
- `IProjectSerializer` → `JsonProjectSerializer` → Singleton
- `IProjectService` → `ProjectService` → Singleton
- `IFileService` → `FileService` → Singleton
- `IDialogService` → `DialogService` → Singleton
- `ITemplatesService` → `TemplatesService` → Singleton

Nota: `TemplateEditorShellViewModel` es Scoped porque se crea una instancia por cada pestaña de editor abierta. `NavigationService` se registra como ambas interfaces (`INavigationService` e `ICommandContext`) porque trackea el documento activo para el Command Routing.
