using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Msi.TemplateCodeGenerator;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        // Borrar el fichero last.log para que solo contenga logs de la ejecución actual
        string lastLogPath = Path.Combine("logs", "Msi.TemplateCodeGenerator-last.log");
        if (File.Exists(lastLogPath))
        {
            try
            {
                File.Delete(lastLogPath);
            }
            catch (Exception ex)
            {
                // Si no se puede borrar (está bloqueado por otra instancia), continuar
                Console.Error.WriteLine($"Warning: Could not delete last.log: {ex.Message}");
            }
        }

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);

        builder.Services.AddTemplateCodeGeneratorServices();

        IHost host = builder.Build();
        App.Services = host.Services;
        host.Start();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .WithInterFont()
                     .LogToTrace();
}
