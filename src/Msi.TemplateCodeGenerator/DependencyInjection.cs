using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services.Project;
using Msi.TemplateCodeGenerator.Services.Templates;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.UI.Services.Commands;
using Msi.TemplateCodeGenerator.UI.Services.Dialogs;
using Msi.TemplateCodeGenerator.UI.Services.Navigation;
using Msi.TemplateCodeGenerator.UI.Views.MetadataEditor.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.Settings.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.Shell;
using Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

namespace Msi.TemplateCodeGenerator;

/// <summary>
/// Provee métodos de extension para el registro de servicios en el IoC
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios del proyecto
    /// </summary>
    public static IServiceCollection AddTemplateCodeGeneratorServices(this IServiceCollection services)
    {
        //----------------------
        // Infraestructura (UI)
        //----------------------
        // Registrar los servicios específicos de la UI
        services.AddSingleton<MainWindow>();

        // Registrar los ViewModels de las "páginas".
        services.AddSingleton<MainShellViewModel>();
        services.AddSingleton<SettingsShellViewModel>();

        // Registrar dock: AppDockFactory (interna) + INavigationService (pública)
        services.AddSingleton<AppDockFactory>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<ICommandContext>(sp => sp.GetRequiredService<NavigationService>());

        // Registrar Dock: tipos de Tools
        services.AddSingleton<ProjectExplorerShellViewModel>();

        // Registrar Dock: tipos de Document
        services.AddScoped<TemplateEditorShellViewModel>();
        services.AddScoped<MetadataEditorShellViewModel>();

        //---------
        // Dominio
        //---------
        // Registrar sistema de mensajería
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // Registrar servicios de proyecto
        services.AddSingleton<IProjectContext, ProjectContext>();
        services.AddSingleton<IProjectSerializer, JsonProjectSerializer>();
        services.AddSingleton<IProjectService, ProjectService>();

        // Registrar otros servicios
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<ITemplatesService, TemplatesService>();

        // Registrar CommandRegistry
        services.AddSingleton<ICommandRegistry, CommandRegistry>();

        return services;
    }
}
