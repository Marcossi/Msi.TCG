using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.UI;
using Msi.TemplateCodeGenerator.UI.Settings;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;
using NLog.Extensions.Logging;

namespace Msi.TemplateCodeGenerator;

/// <summary>
/// Provides extension methods for service registration for the WPF project.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the services from the WPF project into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTemplateCodeGeneratorServices(this IServiceCollection services)
    {
        // Registrar aquí los servicios específicos de la UI
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainShellViewModel>();

        // Registrar los ViewModels de las "páginas".
        // Se registran como Singleton para que mantengan su estado durante la vida de la aplicación.
        services.AddSingleton<TemplateEditorShellViewModel>();
        services.AddSingleton<SettingsShellViewModel>();


        // Configuración de logging con NLog
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace); // Este nivel es del sistema de log de MS. Cedemos el control total a NLog, que tiene su propio nivel (el del nlog.config)
            loggingBuilder.AddNLog("nlog.config");
        });

        return services;
    }
}
