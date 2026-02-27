namespace Msi.TemplateCodeGenerator.Constants;

/// <summary>
/// Identificadores de los paneles del dock.
/// Usados para referenciar dockables sin cadenas literales dispersas.
/// </summary>
public static class NavigationConstants
{
    /// <summary>Panel lateral del explorador de proyectos.</summary>
    public const string ProjectExplorerId = "ProjectExplorer";

    /// <summary>Documento del editor de plantillas.</summary>
    public const string TemplateEditorId = "TemplateEditor";

    /// <summary>Documento de configuración.</summary>
    public const string SettingsId = "Settings";

    // Contenedores internos del layout (uso interno de NavigationService)
    internal const string LeftPaneId      = "LeftPane";
    internal const string DocumentsPaneId = "DocumentsPane";
    internal const string MainLayoutId    = "MainLayout";
    internal const string RootId          = "Root";
}
