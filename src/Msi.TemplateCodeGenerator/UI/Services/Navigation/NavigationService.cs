using System;
using System.IO;
using System.Linq;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

namespace Msi.TemplateCodeGenerator.UI.Services.Navigation;

/// <summary>
/// Implementación del servicio de navegación.
/// Construye el layout del dock y expone operaciones de navegación
/// delegando internamente en AppDockFactory.
/// </summary>
internal sealed class NavigationService : INavigationService
{
    private readonly AppDockFactory _factory;
    private readonly IServiceProvider _serviceProvider;
    private IRootDock? _layout;

    public NavigationService(AppDockFactory factory, IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
        // NO inicializamos el layout aquí para evitar bucle infinito.
        // Se inicializa lazy cuando se solicita por primera vez.
    }

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
        var dockable = FindById(id);
        if (dockable != null)
            _factory.SetActiveDockable(dockable);
    }

    /// <summary>
    /// Oculta un dockable por su ID.
    /// </summary>
    public void HideDockable(string id)
    {
        var dockable = FindById(id);
        if (dockable != null)
            _factory.HideDockable(dockable);
    }

    /// <summary>
    /// Abre un archivo en un nuevo editor como documento (pestaña).
    /// Crea una instancia transitoria de TemplateEditorShellViewModel para cada archivo.
    /// </summary>
    public void OpenFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        // Buscar si ya está abierto
        var existingDoc = _factory.Find(d => d.Id == $"File_{filePath}").FirstOrDefault();
        if (existingDoc != null)
        {
            _factory.SetActiveDockable(existingDoc);
            return;
        }

        // Crear nueva instancia transitoria del ViewModel
        var editorVM = _serviceProvider.GetRequiredService<TemplateEditorShellViewModel>();
        editorVM.LoadFile(filePath);

        var document = new Document
        {
            Id = $"File_{filePath}",
            Title = Path.GetFileName(filePath),
            Context = editorVM,
            CanClose = true
        };

        // Buscar el DocumentDock y añadir el documento
        var documentDock = FindById(NavigationConstants.DocumentsPaneId) as IDocumentDock;
        if (documentDock != null)
        {
            _factory.AddDockable(documentDock, document);
            _factory.SetActiveDockable(document);
        }
    }

    /// <summary>
    /// Busca un dockable por ID recorriendo el layout.
    /// </summary>
    private IDockable? FindById(string id)
        => _factory.Find(d => d.Id == id).FirstOrDefault();
}
