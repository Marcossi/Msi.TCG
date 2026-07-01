using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.Settings.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Services.Navigation;

/// <summary>
/// Fábrica que construye el layout inicial del shell (paneles, pestañas, proporciones).
/// Resuelve ViewModels bajo demanda desde el IoC para evitar dependencias circulares.
/// Usada exclusivamente por NavigationService.
/// </summary>
internal sealed class AppDockFactory(
    IServiceProvider serviceProvider,
    ILogger<AppDockFactory> logger) : Factory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<AppDockFactory> _logger = logger;

    /// <summary>
    /// Construye el layout completo de la aplicación.
    /// Resuelve los ViewModels bajo demanda para evitar dependencias circulares.
    /// </summary>
    public override IRootDock CreateLayout()
    {
        _logger.LogInformation("Construyendo layout del dock");

        // Resolver ViewModels bajo demanda (lazy resolution)
        ProjectExplorerShellViewModel projectExplorer = _serviceProvider.GetRequiredService<ProjectExplorerShellViewModel>();
        TemplateEditorShellViewModel templateEditor = _serviceProvider.GetRequiredService<TemplateEditorShellViewModel>();
        SettingsShellViewModel settings = _serviceProvider.GetRequiredService<SettingsShellViewModel>();

        _logger.LogDebug("ViewModels resueltos: ProjectExplorer, TemplateEditor, Settings");

        // Estructura del layout:
        // RootDock
        // └── ProportionalDock(MainLayout)
        //     ├── ToolDock(LeftPane)
        //     │   └── Tool(ProjectExplorer)
        //     ├── Splitter
        //     └── DocumentDock(DocumentsPane)
        //         ├── Document1
        //         ├── Document2
        //         └── ...
        Tool projectExplorerTool = new()
        {
            Id = NavigationConstants.ProjectExplorerId,
            Title = "Explorador de Proyectos",
            Context = projectExplorer
        };

        ToolDock leftToolDock = new()
        {
            Id = NavigationConstants.LeftPaneId,
            Proportion = 0.22,
            ActiveDockable = projectExplorerTool,
            VisibleDockables = CreateList<IDockable>(projectExplorerTool),
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible
        };

        DocumentDock documentDock = new()
        {
            Id = NavigationConstants.DocumentsPaneId,
            Proportion = double.NaN,
            IsCollapsable = false,
            CanCreateDocument = false
        };

        ProportionalDock mainLayout = new()
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
