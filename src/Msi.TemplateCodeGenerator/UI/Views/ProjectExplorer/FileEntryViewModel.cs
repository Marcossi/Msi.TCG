using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.UI.ProjectExplorer;

/// <summary>
/// ViewModel de presentación para una entrada (fichero o directorio) del árbol de proyecto.
/// Se reconstruye completamente en cada refresco, por lo que no necesita notificación de cambios.
/// </summary>
internal sealed class FileEntryViewModel
{
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
    /// Indica si el nodo debe mostrarse expandido al cargar el árbol.
    /// </summary>
    public bool IsExpanded { get; init; }

    /// <summary>
    /// Hijos de este nodo en el árbol (subdirectorios y ficheros contenidos).
    /// </summary>
    public List<FileEntryViewModel> Children { get; } = [];

    public FileEntryViewModel(string name, string relativePath, FileType type)
    {
        Name = name;
        RelativePath = relativePath;
        Type = type;
    }
}
