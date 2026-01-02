using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.UI;
using Serilog;

namespace Msi.TemplateCodeGenerator;

public partial class App : Application
{
    // Guardamos el Host para tenerlo disponible durante el ciclo de vida de la aplicación
    private IHost _host = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Host típico de .Net. Ahora que Avalonia ya se ha inicializado podemos hacer un HostApplication clásico
        //--------------------------------------------------------------------------------------------------------

        // Configurar y Construir el Host de la aplicación
        var builder = Host.CreateApplicationBuilder();
        InitializeConfiguration(builder);
        InitializeLogging(builder);
        InitializeServices(builder);
        _host = builder.Build();
        _host.Start();

        // Mostrar el Banner de inicio
        LogStartupBanner(_host.Services);


        // Terminamos la inicializacion de Avalonia
        //------------------------------------------
        // En este caso sabemos que la ejecución es de escritorio clásico
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            // Evitar validaciones duplicadas
            Avalonia_DisableDataAnnotationValidation();
            Avalonia_CreateMainWindow(desktopApp);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void InitializeConfiguration(HostApplicationBuilder builder)
    {
        // Aseguramos que se cargue appsettings.json con recarga en caliente
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    }

    private static void InitializeLogging(HostApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);
    }

    private static void InitializeServices(HostApplicationBuilder builder)
    {
        // Registrar servicios propios y de Avalonia
        builder.Services.AddTemplateCodeGeneratorServices();
    }

    private static void LogStartupBanner(IServiceProvider services)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<App>>();
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            
            logger.LogInformation("""
                          -----------------------
                           TemplateCodeGenerator  v{Version}
                          -----------------------
                          Start application...
                          """,
                          version);
        }
        catch
        {
            // Si falla el log en el arranque, no queremos que la app explote, 
            // pero es muy raro que falle si el Host se construyó bien.
        }
    }

    private static void Avalonia_DisableDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void Avalonia_CreateMainWindow(IClassicDesktopStyleApplicationLifetime desktopApp)
    {
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var mainShellViewModel = _host.Services.GetRequiredService<MainShellViewModel>();

        mainWindow.DataContext = mainShellViewModel;
        desktopApp.MainWindow = mainWindow;

        // Manejar el cierre de la aplicación para detener el Host
        desktopApp.Exit += (sender, args) =>
        {
            _host.StopAsync().Wait();
            _host.Dispose();
        };
    }
}