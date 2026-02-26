using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Msi.TemplateCodeGenerator.UI.ProjectExplorer;
using Msi.TemplateCodeGenerator.UI.Settings;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

namespace Msi.TemplateCodeGenerator.UI;

/// <summary>
/// Fábrica que construye el layout inicial del shell (paneles, pestañas, proporciones).
/// No instancia ViewModels: los recibe del IoC y los asigna como Context de cada panel.
/// </summary>
internal sealed class AppDockFactory : Factory
{
    private readonly ProjectExplorerShellViewModel _projectExplorer;
    private readonly TemplateEditorShellViewModel _templateEditor;
    private readonly SettingsShellViewModel _settings;

    public AppDockFactory(
        ProjectExplorerShellViewModel projectExplorer,
        TemplateEditorShellViewModel templateEditor,
        SettingsShellViewModel settings)
    {
        _projectExplorer = projectExplorer;
        _templateEditor = templateEditor;
        _settings = settings;
    }

    /// <summary>
    /// Construye el layout completo de la aplicación.
    /// </summary>
    public override IRootDock CreateLayout()
    {
        // Paneles de tipo Tool (laterales, ocultables)
        var projectExplorerTool = new Tool
        {
            Id = "ProjectExplorer",
            Title = "Explorador de Proyectos",
            Context = _projectExplorer
        };

        // Paneles de tipo Document (área central con pestañas)
        var templateEditorDocument = new Document
        {
            Id = "TemplateEditor",
            Title = "Editor de Plantillas",
            Context = _templateEditor
        };

        var settingsDocument = new Document
        {
            Id = "Settings",
            Title = "Configuración",
            Context = _settings
        };

        // Panel izquierdo de herramientas
        var leftToolDock = new ToolDock
        {
            Id = "LeftPane",
            Proportion = 0.22,
            ActiveDockable = projectExplorerTool,
            VisibleDockables = CreateList<IDockable>(projectExplorerTool),
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible
        };

        // Área central de documentos con pestañas
        var documentDock = new DocumentDock
        {
            Id = "DocumentsPane",
            Proportion = double.NaN,
            ActiveDockable = templateEditorDocument,
            VisibleDockables = CreateList<IDockable>(templateEditorDocument, settingsDocument),
            CanCreateDocument = false
        };

        // Layout principal: horizontal (izquierda | centro)
        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                leftToolDock,
                new ProportionalDockSplitter(),
                documentDock
            )
        };

        // Raíz del dock
        var rootDock = new RootDock
        {
            Id = "Root",
            IsCollapsable = false,
            ActiveDockable = mainLayout,
            DefaultDockable = mainLayout,
            VisibleDockables = CreateList<IDockable>(mainLayout)
        };

        return rootDock;
    }
}
