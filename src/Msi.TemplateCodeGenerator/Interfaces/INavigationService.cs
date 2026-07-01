using Dock.Model.Controls;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio de navegación del shell. Gestiona el layout de paneles y la
/// activación de documentos y herramientas.
/// Abstrae la implementación concreta de Dock.Avalonia del resto de la aplicación.
/// </summary>
/// <remarks>
/// Nota de diseño: GetLayout() devuelve IRootDock como concesión necesaria
/// para el binding del DockControl en MainShellViewModel. Solo el ShellViewModel
/// usa este método; el resto de la aplicación solo usa ActivateDockable/HideDockable.
/// </remarks>
public interface INavigationService
{
    /// <summary>
    /// Devuelve el objeto que gestiona las ventanas del shell. La UI reflejará su estructura lógica en forma de paneles, pestañas, ventanas acopables, etc
    /// </summary>
    IRootDock GetLayout();

    /// <summary>
    /// Activa y trae al foco un panel por su ID (ver NavigationConstants).
    /// </summary>
    void ActivateDockable(string id);

    /// <summary>
    /// Oculta un dockable por su ID.
    /// </summary>
    void HideDockable(string id);

    /// <summary>
    /// Abre un archivo en un nuevo editor como documento (pestaña).
    /// </summary>
    /// <param name="filePath">Ruta del archivo a abrir.</param>
    Task OpenFile(string filePath);

    /// <summary>
    /// Cierra un documento consultando primero si puede cerrarse (si implementa ICloseAware).
    /// </summary>
    /// <param name="documentId">ID del documento a cerrar.</param>
    /// <returns>true si el cierre fue exitoso; false si fue abortado.</returns>
    Task<bool> CloseDocumentAsync(string documentId);

    /// <summary>
    /// Comprueba si todos los documentos abiertos pueden cerrarse de forma segura.
    /// </summary>
    Task<bool> CanCloseAllAsync();

    /// <summary>
    /// Obtiene una enumeración de todos los ViewModels de editores abiertos.
    /// </summary>
    IEnumerable<ICloseAware> GetOpenEditors();
}
