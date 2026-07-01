# MSI Guidelines - Bootstrap Avalonia Desktop

> Capa específica para aplicaciones de escritorio Avalonia. Extiende `msi-bootstrap-hosting.md` con el patrón de arranque visual, separación de builders y diseño en tiempo de ejecución.

## 1. Relación con el Host Genérico
- El arranque de Avalonia **NO sustituye** al Host genérico de .NET. Ambos conviven como builders independientes.
- **Regla de separación:** El .NET Host gestiona infraestructura (DI, Serilog, configuración). El Avalonia AppBuilder gestiona el framework gráfico (plataforma, fuentes, dev tools).
- El diseñador visual **solo ejecuta `BuildAvaloniaApp()`**. No crea el .NET Host ni resuelve DI.

## 2. Patrón de Program.cs
- Crear el .NET Host primero con la secuencia canónica de `msi-bootstrap-hosting.md`.
- Registrar `IApp` como factory lazy: `builder.Services.AddSingleton<IApp>(_ => (IApp)Application.Current!);`
- Construir el host: `var host = builder.Build();`
- Pasar `host.Services` a `App.Services` (propiedad estática).
- Lanzar Avalonia: `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);`
- `BuildAvaloniaApp()` debe mantenerse limpio: solo `AppBuilder.Configure<App>()`, `UsePlatformDetect()`, fuentes y dev tools.

## 3. Patrón de App.axaml.cs
- `App` implementa `IApp` y expone `public static IServiceProvider? Services { get; internal set; }`.
- `OnFrameworkInitializationCompleted` **consume** `App.Services`, nunca crea un `ServiceCollection` nuevo.
- Resolver el ViewModel principal desde `App.Services.GetRequiredService<MainWindowViewModel>()`.
- Asignar `DataContext` y `MainWindow`/`MainView` según `ApplicationLifetime`.
- `Shutdown()` debe usar `IHostApplicationLifetime.StopApplication()` resuelto desde `Services`.

### Nota sobre IApp

En aplicaciones con una sola ventana principal y sin necesidad de orquestar múltiples shells, `IApp` puede omitirse. El bootstrap resuelve `MainWindow` y su ViewModel directamente desde `App.Services`. Solo introducir `IApp` cuando hay operaciones globales que justifican una fachada (múltiples ventanas, estado de aplicación compartido, shutdown coordinado).

## 4. Interfaz IApp (Fachada Global)
- `IApp` representa operaciones globales de la shell (Shutdown, navegación, estado de ventana activa).
- **No envolver todo:** Solo expone lo que los ViewModels realmente necesitan.
- La implementación es la propia `App : Application, IApp`.
- Registrada como singleton lazy en `Program.cs`.

## 5. Patrón de ViewModel en Tiempo de Diseño
- El ViewModel real **NO** necesita constructor sin parámetros ni null checks defensivos.
- Crear una clase `*ViewModelDesign` **completamente separada** (no hereda del real).
- El Design ViewModel implementa sus propias `[ObservableProperty]` y `[RelayCommand]` con datos hardcodeados.
- Los comandos del Design son stubs vacíos.
- En el XAML: `<Design.DataContext><vm:MainWindowViewModelDesign /></Design.DataContext>` (nativo de Avalonia).
- Prohibido usar `d:DesignInstance` del namespace Blend. Avalonia no lo soporta en el previewer.

## 6. Registro DI Específico para Avalonia
- ViewModels se registran como `Singleton` (una instancia por ventana/control).
- Servicios de UI (`INavigationService`, `IDialogService`) como `Singleton`.
- `IServiceProvider` se auto-registra si es necesario: `services.AddSingleton<IServiceProvider>(sp => sp);`
- No registrar `IHost` en el contenedor. Usar `IHostApplicationLifetime` para ciclo de vida.

## 7. Logging en Bootstrap

- `App.axaml.cs` debe inyectar `ILogger<App>` para el banner de arranque y errores.
- La resolución del entry point (`MainWindow`, `MainShellViewModel`) debe envolverse en try-catch con logging.
- **Prohibido** `catch {}` vacío. Todo catch debe loguear la excepción:

```csharp
try
{
    MainWindow mainWindow = services.GetRequiredService<MainWindow>();
    MainShellViewModel shellVm = services.GetRequiredService<MainShellViewModel>();
    mainWindow.DataContext = shellVm;
    desktop.MainWindow = mainWindow;
    mainWindow.Show();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Error al inicializar la ventana principal");
    throw;
}
```

## 8. Checklist de Arranque
1. Host genérico configurado (Serilog, appsettings, DI).
2. `IApp` registrado como lazy factory (si aplica).
3. `host.Build()` ejecutado.
4. `App.Services = host.Services` asignado.
5. `BuildAvaloniaApp()` limpio y sin DI.
6. `OnFrameworkInitializationCompleted` resuelve ViewModel del contenedor.
7. Design ViewModel separado y referenciado en XAML con `<Design.DataContext>`.
8. Resolución del entry point envuelta en try-catch con logging. Prohibido `catch {}` vacío.
