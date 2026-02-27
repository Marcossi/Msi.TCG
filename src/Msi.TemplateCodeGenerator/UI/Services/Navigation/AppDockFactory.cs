using System;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.UI.ProjectExplorer;
using Msi.TemplateCodeGenerator.UI.Settings;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

namespace Msi.TemplateCodeGenerator.UI.Services.Navigation;

/// <summary>
/// Fábrica que construye el layout inicial del shell (paneles, pestañas, proporciones).
/// Resuelve ViewModels bajo demanda desde el IoC para evitar dependencias circulares.
/// Usada exclusivamente por NavigationService.
/// </summary>
internal sealed class AppDockFactory : Factory
{
    private readonly IServiceProvider _serviceProvider;

    public AppDockFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Construye el layout completo de la aplicación.
    /// Resuelve los ViewModels bajo demanda para evitar dependencias circulares.
    /// </summary>
    public override IRootDock CreateLayout()
    {
        // Resolver ViewModels bajo demanda (lazy resolution)
        var projectExplorer = _serviceProvider.GetRequiredService<ProjectExplorerShellViewModel>();
        var templateEditor = _serviceProvider.GetRequiredService<TemplateEditorShellViewModel>();
        var settings = _serviceProvider.GetRequiredService<SettingsShellViewModel>();

        // Estructura del layout:
        // RootDock
        // └── ProportionalDock(MainLayout)
        //     ├── ToolDock(LeftPane)
        //     │   └── Tool(ProjectExplorer)
        //     ├── Splitter
        //     └── DocumentDock(DocumentsPane)
        //         ├── Document(TemplateEditor)
        //         └── Document(Settings)
        var projectExplorerTool = new Tool
        {
            Id = NavigationConstants.ProjectExplorerId,
            Title = "Explorador de Proyectos",
            Context = projectExplorer
        };

        var templateEditorDocument = new Document
        {
            Id = NavigationConstants.TemplateEditorId,
            Title = "Editor de Plantillas",
            Context = templateEditor
        };

        var settingsDocument = new Document
        {
            Id = NavigationConstants.SettingsId,
            Title = "Configuración",
            Context = settings
        };

        var leftToolDock = new ToolDock
        {
            Id = NavigationConstants.LeftPaneId,
            Proportion = 0.22,
            ActiveDockable = projectExplorerTool,
            VisibleDockables = CreateList<IDockable>(projectExplorerTool),
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible
        };

        var documentDock = new DocumentDock
        {
            Id = NavigationConstants.DocumentsPaneId,
            Proportion = double.NaN,
            IsCollapsable = false,
            ActiveDockable = templateEditorDocument,
            VisibleDockables = CreateList<IDockable>(templateEditorDocument, settingsDocument),
            CanCreateDocument = false
        };

        var mainLayout = new ProportionalDock
        {
            Id = NavigationConstants.MainLayoutId,
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                leftToolDock,
                new ProportionalDockSplitter(),
                documentDock
            )
        };

        return new RootDock
        {
            Id = NavigationConstants.RootId,
            IsCollapsable = false,
            ActiveDockable = mainLayout,
            DefaultDockable = mainLayout,
            VisibleDockables = CreateList<IDockable>(mainLayout)
        };
    }
}
