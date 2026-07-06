# ADR-004: Fachada de Aplicación (IApp) y Alineación del Bootstrap

## Estado

Accepted (2026-07-03)

## Contexto

El bootstrap actual del proyecto está **invertido** respecto a las guías de MSI (`msi-bootstrap-avalonia.md`):

- El .NET Host se crea dentro de `App.OnFrameworkInitializationCompleted` en lugar de en `Program.Main`.
- No existe `IApp` como fachada global para operaciones de la shell.
- El comando `Exit` en `MainShellViewModel` solo loguea, no cierra la aplicación.

**Fuerzas:**
- Necesidad de un cierre controlado que consulte `CanCloseAllAsync()` antes de cerrar.
- Regla de arquitectura: prohibido acceder a `Avalonia.*` desde ViewModels.
- Coherencia con las guías de bootstrap de MSI.
- Testabilidad: `IApp` permite mockear el shutdown en tests.

## Decisión

### 1. Introducir `IApp` como fachada global mínima

```csharp
public interface IApp
{
    void Shutdown();
}
```

La implementación es la propia `App : Application, IApp`.

`Shutdown()` usa `IHostApplicationLifetime.StopApplication()` resuelto desde `App.Services`.

### 2. Reestructurar el bootstrap según las guías

**`Program.cs`**: Crear el .NET Host antes de lanzar Avalonia.

```csharp
[STAThread]
public static void Main(string[] args)
{
    IHost host = Host.CreateApplicationBuilder(args)
        .ConfigureAppConfiguration(...)
        .ConfigureLogging(...)
        .ConfigureServices(...)
        .Build();

    App.Services = host.Services;
    host.Start();

    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
```

**`App.axaml.cs`**: Implementar `IApp`, exponer `Services` estático, consumir en `OnFrameworkInitializationCompleted`.

```csharp
public partial class App : Application, IApp
{
    public static IServiceProvider? Services { get; internal set; }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = Services!.GetRequiredService<MainWindow>();
            MainShellViewModel shellVm = Services.GetRequiredService<MainShellViewModel>();
            mainWindow.DataContext = shellVm;
            desktop.MainWindow = mainWindow;
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void Shutdown()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
```

**`DependencyInjection.cs`**: Registrar `IApp` como lazy factory.

```csharp
services.AddSingleton<IApp>(_ => (IApp)Application.Current!);
```

### 3. Implementar `ExitAsync()` en `MainShellViewModel`

Inyectar `IApp` + `INavigationService` como campos.

```csharp
[RelayCommand]
private async Task ExitAsync()
{
    _logger.LogInformation("[UI] Command: Exit");
    bool canClose = await _navigationService.CanCloseAllAsync();
    if (canClose)
    {
        _app.Shutdown();
    }
}
```

### 4. Criterio de clasificación de operaciones

Las operaciones se clasifican en dos categorías mutuamente excluyentes:

- **Globales**: No dependen del documento activo. Llamada directa a servicios.
- **Contextuales**: Dependen del dockable con foco. Routing obligatorio vía `ICommandRegistry`.

**Criterio**: ¿La operación necesita saber qué documento tiene foco para ejecutarse correctamente? Si sí → contextual. Si no → global.

| Operación | Categoría | Patrón |
|---|---|---|
| New/Open/Close/SaveProject | Global | Llamada directa a `IProjectService` |
| Save (editor) | Contextual | `ICommandRegistry` → `ICommandRoute` |
| OpenFile desde tree | Global | Llamada directa a `INavigationService` |
| RefreshFiles | Global | Llamada directa a `IProjectService` |
| Exit | Global | `IApp.Shutdown()` |

## Alternativas consideradas

### Alternativa A: Mantener bootstrap actual + `Application.Current.Shutdown()` directo

**Descartada porque:**
- Viola la regla de no acceder a `Avalonia.*` desde ViewModels.
- No testable: no se puede mockear `Application.Current`.
- Incoherente con las guías de bootstrap de MSI.

### Alternativa B: `IApp` con más operaciones (ShowWindow, GetMainWindow, etc.)

**Descartada porque:**
- YAGNI: no hay casos de uso actuales que justifiquen más operaciones.
- Crecer por necesidades reales, no por especulación.

## Consecuencias

### Positivas
- Coherencia con las guías de bootstrap de MSI.
- Testabilidad: `IApp` permite mockear el shutdown.
- Cierre controlado: consulta `CanCloseAllAsync()` antes de cerrar.
- Fachada mínima: solo lo necesario, crece por necesidades reales.

### Negativas
- Más trabajo inicial: reestructurar bootstrap, crear `IApp`, migrar `Program.cs` y `App.axaml.cs`.
- Curva de aprendizaje: los desarrolladores deben entender el patrón de `IApp`.

### Riesgos mitigados
- **Bootstrap invertido**: La reestructuración alinea el proyecto con las guías.
- **Exit no implementado**: Se implementa con cierre controlado.
- **Acceso a `Avalonia.*` desde VM**: `IApp` abstrae el shutdown.

## Referencias

- Guías de bootstrap: `docs/agents/msi-guidelines-avalonia/msi-bootstrap-avalonia.md`
- Especificación de implementación: `docs/agents/proyecto/especificaciones/bootstrap-alignment.md`
- ADR relacionado: `ADR-001-command-routing.md`
