using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Shared;

namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

/// <summary>
/// ViewModel de presentación para una entrada (fichero o directorio) del árbol de proyecto.
/// Se reconstruye completamente en cada refresco; el estado de expansión se preserva
/// capturando los paths expandidos antes de reconstruir y restaurándolos después.
/// </summary>
internal sealed partial class FileEntryViewModel : BaseViewModel
{
    private readonly ILogger<FileEntryViewModel>? _logger;

    /// <summary>
    /// Nombre del fichero o directorio.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Ruta relativa normalizada desde la carpeta raíz del proyecto.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Tipo de entrada en el sistema de archivos.
    /// </summary>
    public FileType Type { get; }

    /// <summary>
    /// Indica si el nodo está expandido en el TreeView.
    /// Two-way binding con TreeViewItem.IsExpanded para preservar el estado del usuario.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Hijos de este nodo en el árbol (subdirectorios y ficheros contenidos).
    /// </summary>
    public List<FileEntryViewModel> Children { get; } = [];

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Indica si el nodo está en modo de edición inline (rename/create).
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Nombre en edición durante el modo inline.
    /// </summary>
    [ObservableProperty]
    private string _editingName = string.Empty;

    public FileEntryViewModel(string name, string relativePath, FileType type, ILogger<FileEntryViewModel>? logger = null)
    {
        Name = name;
        RelativePath = relativePath;
        Type = type;
        _logger = logger;
    }

    public void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        _logger?.LogWarning("Error en entrada '{Path}': {Message}", RelativePath, message);
    }

    public void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }
}
