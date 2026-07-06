using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Views.Shell;
using Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;

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
            IServiceProvider services = Services
                ?? throw new InvalidOperationException("App.Services no ha sido inicializado.");

            logger = services.GetRequiredService<ILogger<App>>();
            LogStartupBanner(logger);

            MainWindow mainWindow = services.GetRequiredService<MainWindow>();
            MainShellViewModel shellVm = services.GetRequiredService<MainShellViewModel>();

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
