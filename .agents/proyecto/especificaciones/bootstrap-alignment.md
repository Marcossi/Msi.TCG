# Especificación técnica: Alineación del Bootstrap con IApp

## Propósito

Reestructurar el bootstrap del proyecto para alinearlo con las guías de MSI (`msi-bootstrap-avalonia.md`) e introducir `IApp` como fachada global para operaciones de la shell.

## Referencia

- ADR: `.agents/proyecto/adr/ADR-004-app-facade-and-bootstrap.md`
- Guías de bootstrap: `.agents/msi-guidelines-avalonia/msi-bootstrap-avalonia.md`

## Cambios detallados

### 1. Crear interfaz `IApp`

**Ubicación:** `Interfaces/IApp.cs`

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Fachada global para operaciones de la shell.
/// Expone únicamente lo que los ViewModels realmente necesitan.
/// </summary>
public interface IApp
{
    /// <summary>
    /// Cierra la aplicación de forma controlada.
    /// </summary>
    void Shutdown();
}
```

### 2. Modificar `Program.cs`

**Ubicación:** `src/Msi.TemplateCodeGenerator/Program.cs`

**Estado actual:** El .NET Host se crea dentro de `App.OnFrameworkInitializationCompleted`.

**Nuevo estado:** Crear el .NET Host en `Program.Main` antes de lanzar Avalonia.

```csharp
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator;
using Serilog;

namespace Msi.TemplateCodeGenerator;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. Crear el .NET Host antes de lanzar Avalonia
        IHost host = Host.CreateApplicationBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureLogging((context, logging) =>
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(context.Configuration)
                    .CreateLogger();

                logging.ClearProviders();
                logging.AddSerilog(dispose: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddTemplateCodeGeneratorServices();
            })
            .Build();

        // 2. Pasar host.Services a App.Services
        App.Services = host.Services;

        // 3. Iniciar el Host
        host.Start();

        // 4. Lanzar Avalonia
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .WithInterFont()
                     .LogToTrace();
}
```

### 3. Modificar `App.axaml.cs`

**Ubicación:** `src/Msi.TemplateCodeGenerator/App.axaml.cs`

**Estado actual:** Crea el Host, lo inicia, resuelve MainWindow y MainShellViewModel.

**Nuevo estado:** Implementar `IApp`, exponer `Services` estático, consumir en `OnFrameworkInitializationCompleted`.

```csharp
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Views.Shell;
using Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;
using Serilog;

namespace Msi.TemplateCodeGenerator;

public partial class App : Application, IApp
{
    /// <summary>
    /// Proveedor de servicios global. Se asigna en Program.Main antes de lanzar Avalonia.
    /// </summary>
    public static IServiceProvider? Services { get; internal set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Avalonia_CreateMainWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Avalonia_CreateMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ILogger<App>? logger = null;
        try
        {
            logger = Services!.GetRequiredService<ILogger<App>>();
            LogStartupBanner(logger);

            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();
            MainShellViewModel shellVm = Services.GetRequiredService<MainShellViewModel>();

            mainWindow.DataContext = shellVm;
            desktop.MainWindow = mainWindow;
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Error al inicializar la ventana principal");
            throw;
        }
    }

    private static void LogStartupBanner(ILogger<App> logger)
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        
        logger.LogInformation("""
                      -----------------------
                       TemplateCodeGenerator  v{Version}
                      -----------------------
                      Start application...
                      """,
                      version);
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
```

### 4. Modificar `DependencyInjection.cs`

**Ubicación:** `src/Msi.TemplateCodeGenerator/DependencyInjection.cs`

**Añadir:** Registro de `IApp` como lazy factory.

```csharp
public static IServiceCollection AddTemplateCodeGeneratorServices(this IServiceCollection services)
{
    //----------------------
    // Infraestructura (UI)
    //----------------------
    services.AddSingleton<MainWindow>();

    // Registrar IApp como lazy factory (se resuelve cuando Application.Current está disponible)
    services.AddSingleton<IApp>(_ => (IApp)Avalonia.Application.Current!);

    // Registrar los ViewModels de las "páginas".
    services.AddSingleton<MainShellViewModel>();
    services.AddSingleton<SettingsShellViewModel>();

    // ... resto del registro existente ...
}
```

**Nota:** El registro de `IApp` como lazy factory funciona porque `AddTemplateCodeGeneratorServices()` se invoca antes de que `Application.Current` esté disponible, pero el factory delegate se ejecuta solo cuando se resuelve `IApp` (después de que Avalonia haya inicializado `Application.Current`).

### 5. Modificar `MainShellViewModel.cs`

**Ubicación:** `src/Msi.TemplateCodeGenerator/UI/Views/Shell/ViewModels/MainShellViewModel.cs`

**Estado actual:** No guarda `INavigationService` como campo, `Exit()` solo loguea.

**Nuevo estado:** Inyectar `IApp` + `INavigationService` como campos, implementar `ExitAsync()` con cierre controlado.

```csharp
internal partial class MainShellViewModel : BaseViewModel
{
    private readonly IProjectService _projectService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ICommandRegistry _commandRegistry;
    private readonly INavigationService _navigationService;
    private readonly IApp _app;
    private readonly ILogger<MainShellViewModel> _logger;

    [ObservableProperty]
    private IRootDock? _layout;

    [ObservableProperty]
    private string? _statusMessage;

    public MainShellViewModel(
        INavigationService navigationService,
        IProjectService projectService,
        IFileDialogService fileDialogService,
        ICommandRegistry commandRegistry,
        IApp app,
        ILogger<MainShellViewModel> logger)
    {
        _navigationService = navigationService;
        _projectService = projectService;
        _fileDialogService = fileDialogService;
        _commandRegistry = commandRegistry;
        _app = app;
        _logger = logger;
        _layout = navigationService.GetLayout();
    }

    // ... comandos existentes (NewProject, OpenProject, etc.) ...

    /// <summary>
    /// Cierra la aplicación de forma controlada.
    /// Consulta CanCloseAllAsync() antes de cerrar para permitir guardar cambios pendientes.
    /// </summary>
    [RelayCommand]
    private async Task ExitAsync()
    {
        _logger.LogInformation("[UI] Command: Exit");
        try
        {
            bool canClose = await _navigationService.CanCloseAllAsync();
            if (canClose)
            {
                _logger.LogInformation("[UI] Cierre de aplicación confirmado");
                _app.Shutdown();
            }
            else
            {
                _logger.LogInformation("[UI] Cierre de aplicación cancelado por el usuario");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing Exit");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // ... resto de comandos existentes ...
}
```

## Flujo de Shutdown

```
1. Usuario hace clic en "Salir" en el menú
2. MainShellView invoca MainShellViewModel.ExitCommand
3. ExitAsync() llama a _navigationService.CanCloseAllAsync()
4. NavigationService itera sobre todos los editores abiertos
5. Para cada editor que implementa ICloseAware:
   a. Si IsDirty == true → muestra diálogo de confirmación
   b. Si usuario elige "Cancelar" → CanCloseAllAsync() devuelve false
   c. Si usuario elige "Guardar" → guarda el archivo
   d. Si usuario elige "No guardar" → procede
6. Si CanCloseAllAsync() devuelve true → _app.Shutdown()
7. IApp.Shutdown() → IClassicDesktopStyleApplicationLifetime.Shutdown()
8. Avalonia cierra la ventana principal y dispara el evento Exit
```

## Testing

### Unit tests

- **ExitAsync con CanCloseAllAsync == true**: Verificar que se llama a `IApp.Shutdown()`.
- **ExitAsync con CanCloseAllAsync == false**: Verificar que NO se llama a `IApp.Shutdown()`.
- **IApp.Shutdown()**: Verificar que se llama a `IHostApplicationLifetime.StopApplication()`.

### Integration tests

- **Cierre con editores dirty**: Abrir un editor, modificar contenido, hacer clic en "Salir", verificar que aparece el diálogo de confirmación.
- **Cierre sin editores dirty**: Cerrar la aplicación sin cambios pendientes, verificar que se cierra inmediatamente.

## Consideraciones

### Registro de IApp

El registro de `IApp` como lazy factory funciona porque:
1. `AddTemplateCodeGeneratorServices()` se invoca en `Program.Main` antes de lanzar Avalonia.
2. El factory delegate `(_ => (IApp)Application.Current!)` no se ejecuta hasta que se resuelve `IApp`.
3. `IApp` se resuelve por primera vez cuando se construye `MainShellViewModel`, que ocurre en `App.OnFrameworkInitializationCompleted`, después de que Avalonia haya inicializado `Application.Current`.

### Manejo de errores en Shutdown

Si `ApplicationLifetime` no es `IClassicDesktopStyleApplicationLifetime`, `Shutdown()` no hace nada. Esto es aceptable porque la aplicación es de escritorio clásico y siempre tendrá ese lifetime.

### Compatibilidad con el diseñador visual

El diseñador visual de Avalonia solo ejecuta `BuildAvaloniaApp()`, no `Program.Main`. Por lo tanto:
- `App.Services` será null en el diseñador.
- Los ViewModels no se resolverán desde DI en el diseñador.
- Para el diseño visual, se deben usar Design ViewModels separados (ver `msi-bootstrap-avalonia.md` sección 5).

## Referencias

- ADR: `.agents/proyecto/adr/ADR-004-app-facade-and-bootstrap.md`
- Guías de bootstrap: `.agents/msi-guidelines-avalonia/msi-bootstrap-avalonia.md`
- Guías de MVVM: `.agents/msi-guidelines-avalonia/msi-arquitectura-mvvm.md`
