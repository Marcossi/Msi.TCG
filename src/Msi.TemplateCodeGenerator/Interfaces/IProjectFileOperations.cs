using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio para operaciones de archivo en el explorador de proyectos.
/// </summary>
internal interface IProjectFileOperations
{
    /// <summary>
    /// Busca una entrada de archivo en el árbol por su ruta relativa.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    /// <param name="relativePath">Ruta relativa del archivo a buscar.</param>
    /// <returns>La entrada encontrada o null si no existe.</returns>
    FileEntryViewModel? FindFileEntryByRelativePath(IEnumerable<FileEntryViewModel> fileTree, string relativePath);

    /// <summary>
    /// Resuelve la ruta relativa del directorio padre a partir del nodo seleccionado.
    /// </summary>
    /// <param name="parent">Nodo padre o null para la raíz.</param>
    /// <returns>Ruta relativa del padre o cadena vacía para la raíz.</returns>
    string ResolveParentRelativePath(FileEntryViewModel? parent);

    /// <summary>
    /// Cancela cualquier edición activa en todo el árbol de archivos.
    /// </summary>
    /// <param name="fileTree">Árbol de archivos del proyecto.</param>
    void CancelAllEditing(IEnumerable<FileEntryViewModel> fileTree);

    /// <summary>
    /// Determina si una ruta es un ancestro de otra (para validar drag and drop).
    /// </summary>
    /// <param name="ancestorPath">Ruta del posible ancestro.</param>
    /// <param name="descendantPath">Ruta del posible descendiente.</param>
    /// <returns>True si ancestorPath es ancestro de descendantPath.</returns>
    bool IsAncestorOf(string ancestorPath, string descendantPath);

    /// <summary>
    /// Valida si un target es válido para drop en drag and drop.
    /// </summary>
    /// <param name="sourcePath">Ruta del origen del drag.</param>
    /// <param name="target">Entrada target del drop.</param>
    /// <returns>True si el drop es válido.</returns>
    bool IsValidDropTarget(string sourcePath, FileEntryViewModel? target);
}
