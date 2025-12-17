using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.UI;

namespace Msi.TemplateCodeGenerator;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Crear el constructor del host.
        var builder = Host.CreateApplicationBuilder();

        // Registrar los servicios de los distintos proyectos
        builder.Services.AddTemplateCodeGeneratorServices();

        // Construir el host.
        _host = builder.Build();
        await _host.StartAsync();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application start");

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application stopping");

        using (_host)
        {
            await _host.StopAsync();
            logger.LogInformation("Application stopped");
        }

        base.OnExit(e);
    }
}

