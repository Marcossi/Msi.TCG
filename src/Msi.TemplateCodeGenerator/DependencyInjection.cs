using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services.Project;
using Msi.TemplateCodeGenerator.Services.Templates;
using Msi.TemplateCodeGenerator.UI;
using Msi.TemplateCodeGenerator.UI.ProjectExplorer;
using Msi.TemplateCodeGenerator.UI.Settings;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

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
        // Registrar los servicios específicos de la UI
        services.AddSingleton<MainWindow>();

        // Registrar los ViewModels de las "páginas".
        services.AddSingleton<MainShellViewModel>();
        services.AddSingleton<TemplateEditorShellViewModel>();
        services.AddSingleton<SettingsShellViewModel>();
        services.AddSingleton<ProjectExplorerShellViewModel>();

        // Registrar sistema de mensajería
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // Registrar servicios de proyecto
        services.AddSingleton<IProjectContext, ProjectContext>();
        services.AddSingleton<IProjectSerializer, JsonProjectSerializer>();
        services.AddSingleton<IProjectService, ProjectService>();

        // Registrar otros servicios
        services.AddSingleton<ITemplatesService, TemplatesService>();

        return services;
    }
}
