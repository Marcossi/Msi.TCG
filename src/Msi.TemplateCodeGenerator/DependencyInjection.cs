using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.Services.Project;
using Msi.TemplateCodeGenerator.Services.Templates;
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

        // Registrar IApp como lazy factory (se resuelve cuando Application.Current está disponible)
        services.AddSingleton<IApp>(_ => (IApp)Avalonia.Application.Current!);

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
        services.AddSingleton<ProjectContext>();
        services.AddSingleton<IProjectContext>(sp => sp.GetRequiredService<ProjectContext>());
        services.AddSingleton<IProjectContextMutator>(sp => sp.GetRequiredService<ProjectContext>());
        services.AddSingleton<IProjectSerializer, JsonProjectSerializer>();
        services.AddSingleton<IElementCatalog, ElementCatalog>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IProjectTreeBuilder, ProjectTreeBuilder>();

        // Registrar otros servicios
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IFileWatcherService, FileWatcherService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IProjectExplorerStateService, ProjectExplorerStateService>();
        services.AddSingleton<IProjectExplorerStateManager, ProjectExplorerStateManager>();
        services.AddSingleton<IProjectScriptFinder, ProjectScriptFinder>();
        services.AddSingleton<IProjectFileOperations, ProjectFileOperations>();
        services.AddSingleton<IInlineEditingService, InlineEditingService>();
        services.AddSingleton<IContextMenuService, ContextMenuService>();
        services.AddSingleton<IScriptOutputWriter, ScriptOutputWriter>();
        services.AddSingleton<IScriptEngine, ScriptEngine>();
        services.AddSingleton<ITemplatesService, TemplatesService>();
        services.AddSingleton<IMetadataService, MetadataService>();

        // Registrar CommandRegistry
        services.AddSingleton<ICommandRegistry, CommandRegistry>();

        return services;
    }
}
