using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Shared;
using Msi.TemplateCodeGenerator.UI.Views.MetadataEditor.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

namespace Msi.TemplateCodeGenerator.UI.Services.Navigation;

/// <summary>
/// Implementación del servicio de navegación.
/// Construye el layout del dock y expone operaciones de navegación
/// delegando internamente en AppDockFactory.
/// </summary>
internal sealed class NavigationService(
    AppDockFactory factory,
    IServiceProvider serviceProvider,
    ILogger<NavigationService> logger) : INavigationService, ICommandContext
{
    private ICommandRoute? _activeRoute;

    public ICommandRoute? ActiveRoute => _activeRoute;
    private readonly AppDockFactory _factory = factory;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<NavigationService> _logger = logger;
    private IRootDock? _layout;
    private readonly Dictionary<string, IServiceScope> _documentScopes = new();

    // NO inicializamos el layout aquí para evitar bucle infinito.
    // Se inicializa lazy cuando se solicita por primera vez.

    /// <summary>
    /// Construye e inicializa el layout del dock (lazy initialization).
    /// </summary>
    private IRootDock EnsureLayoutInitialized()
    {
        if (_layout == null)
        {
            _layout = _factory.CreateLayout();
            _factory.InitLayout(_layout);
        }
        return _layout;
    }

    /// <summary>
    /// Devuelve el layout para el binding en MainShellViewModel.
    /// </summary>
    public IRootDock GetLayout() => EnsureLayoutInitialized();

    /// <summary>
    /// Activa y trae al foco un dockable por su ID.
    /// </summary>
    public void ActivateDockable(string id)
    {
        _logger.LogDebug("Activando dockable '{Id}'", id);
        IDockable? dockable = FindById(id);
        if (dockable != null)
        {
            _factory.SetActiveDockable(dockable);
            OnActiveDockableChanged(dockable);
        }
    }

    /// <summary>
    /// Actualiza el contexto de comandos activos cuando cambia el dockable seleccionado.
    /// </summary>
    private void OnActiveDockableChanged(IDockable? dockable)
    {
        _activeRoute = dockable?.Context as ICommandRoute;
        _logger.LogDebug("ActiveRoute actualizado: {ActiveRoute}", _activeRoute?.GetType().Name ?? "null");
    }

    /// <summary>
    /// Oculta un dockable por su ID.
    /// </summary>
    public void HideDockable(string id)
    {
        _logger.LogDebug("Ocultando dockable '{Id}'", id);
        IDockable? dockable = FindById(id);
        if (dockable != null)
            _factory.HideDockable(dockable);
    }

    /// <summary>
    /// Abre un archivo en un nuevo editor como documento (pestaña).
    /// Crea una instancia transitoria de TemplateEditorShellViewModel para cada archivo.
    /// </summary>
    public async Task OpenFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        _logger.LogInformation("[UI] NavigationService: OpenFile '{FilePath}'", filePath);

        // Buscar si ya está abierto
        IEnumerable<IDockable> existingDocs = _factory.Find(d => d.Id == $"File_{filePath}");
        IDockable? existingDoc = existingDocs.FirstOrDefault();
        if (existingDoc != null)
        {
            _logger.LogDebug("Archivo ya abierto, activando '{FilePath}'", filePath);
            _factory.SetActiveDockable(existingDoc);
            OnActiveDockableChanged(existingDoc);
            return;
        }

        // Crear scope explícito para resolver el ViewModel Scoped
        IServiceScope scope = _serviceProvider.CreateScope();
        BaseViewModel editorVM = ResolveEditor(scope, filePath);
        _logger.LogDebug("Editor resuelto: {EditorType} para '{FilePath}'", editorVM.GetType().Name, filePath);

        await LoadEditorFileAsync(editorVM, filePath);

        Document document = new()
        {
            Id = $"File_{filePath}",
            Title = Path.GetFileName(filePath),
            Context = editorVM,
            CanClose = true
        };

        // Almacenar el scope para disposal posterior al cerrar
        _documentScopes[document.Id] = scope;

        // Buscar el DocumentDock y añadir el documento
        IDocumentDock? documentDock = FindById(NavigationConstants.DocumentsPaneId) as IDocumentDock;
        if (documentDock != null)
        {
            _factory.AddDockable(documentDock, document);
            _factory.SetActiveDockable(document);
            OnActiveDockableChanged(document);
        }

        _logger.LogInformation("[UI] Archivo abierto en editor: '{FilePath}' (Editor={EditorType})",
            filePath, editorVM.GetType().Name);
    }

    /// <summary>
    /// Resuelve el editor adecuado según la extensión del archivo.
    /// </summary>
    private BaseViewModel ResolveEditor(IServiceScope scope, string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        BaseViewModel editor = extension switch
        {
            ".json" when IsMetadataFile(filePath)
                => scope.ServiceProvider.GetRequiredService<MetadataEditorShellViewModel>(),
            _ => scope.ServiceProvider.GetRequiredService<TemplateEditorShellViewModel>()
        };

        _logger.LogDebug("Editor resuelto para extensión '{Extension}': {EditorType}",
            extension, editor.GetType().Name);

        return editor;
    }

    /// <summary>
    /// Determina si un fichero es un metadata JSON comprobando si está dentro de la carpeta metadata/.
    /// </summary>
    private static bool IsMetadataFile(string filePath)
    {
        string normalized = filePath.Replace('\\', Path.DirectorySeparatorChar);
        string metadataSegment = $"{Path.DirectorySeparatorChar}metadata{Path.DirectorySeparatorChar}";
        return normalized.Contains(metadataSegment, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Carga el fichero en el editor. Soporta tanto BaseTextEditorViewModel como subclases.
    /// </summary>
    private static async Task LoadEditorFileAsync(BaseViewModel editorVM, string filePath)
    {
        if (editorVM is UI.Views.TemplateEditor.ViewModels.BaseTextEditorViewModel textEditor)
        {
            await textEditor.LoadFileAsync(filePath);
        }
    }

    /// <summary>
    /// Busca un dockable por ID recorriendo el layout.
    /// </summary>
    private IDockable? FindById(string id)
        => _factory.Find(d => d.Id == id).FirstOrDefault();

    /// <inheritdoc/>
    public async Task<bool> CloseDocumentAsync(string documentId)
    {
        _logger.LogDebug("Cerrando documento '{DocumentId}'", documentId);

        IDockable? dockable = FindById(documentId);
        if (dockable == null)
        {
            _logger.LogWarning("Documento no encontrado '{DocumentId}'", documentId);
            return true;  // No existe, consideramos que "cerró"
        }

        // Si el ViewModel implementa ICloseAware, consulta antes de cerrar
        if (dockable is Document doc && doc.Context is ICloseAware closeAware)
        {
            if (!await closeAware.CanCloseAsync())
            {
                _logger.LogDebug("Cierre abortado por el usuario para '{DocumentId}'", documentId);
                return false;  // Usuario abortó el cierre
            }
        }

        // Procede con el cierre
        _factory.CloseDockable(dockable);

        if (dockable is Document activeDoc && activeDoc.Context == _activeRoute)
        {
            _activeRoute = null;
        }

        // Disposing the scope associated with this document
        if (_documentScopes.TryGetValue(documentId, out IServiceScope? scope))
        {
            scope.Dispose();
            _documentScopes.Remove(documentId);
            _logger.LogDebug("Scope disposed para documento '{DocumentId}'", documentId);
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> CanCloseAllAsync()
    {
        IEnumerable<ICloseAware> openEditors = GetOpenEditors();
        foreach (ICloseAware editor in openEditors)
        {
            if (!await editor.CanCloseAsync())
                return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<ICloseAware> GetOpenEditors()
    {
        IRootDock layout = EnsureLayoutInitialized();
        return _factory.Find(d => d is Document doc && doc.Context is ICloseAware)
            .OfType<Document>()
            .Select(d => (ICloseAware)d.Context!)
            .Where(vm => vm != null);
    }
}
