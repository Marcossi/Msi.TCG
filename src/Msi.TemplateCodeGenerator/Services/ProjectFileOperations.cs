using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IProjectFileOperations"/> para operaciones de archivo.
/// </summary>
internal sealed class ProjectFileOperations : IProjectFileOperations
{
    /// <inheritdoc/>
    public FileEntryViewModel? FindFileEntryByRelativePath(IEnumerable<FileEntryViewModel> fileTree, string relativePath)
    {
        foreach (FileEntryViewModel root in fileTree)
        {
            FileEntryViewModel? found = FindInSubtree(root, relativePath);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <inheritdoc/>
    public string ResolveParentRelativePath(FileEntryViewModel? parent)
    {
        if (parent == null)
            return string.Empty;

        if (parent.Type == FileType.Directory)
            return parent.RelativePath;

        return string.Empty;
    }

    /// <inheritdoc/>
    public void CancelAllEditing(IEnumerable<FileEntryViewModel> fileTree)
    {
        foreach (FileEntryViewModel root in fileTree)
        {
            CancelEditingInSubtree(root);
        }
    }

    private static FileEntryViewModel? FindInSubtree(FileEntryViewModel node, string relativePath)
    {
        if (node.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase))
            return node;

        foreach (FileEntryViewModel child in node.Children)
        {
            FileEntryViewModel? found = FindInSubtree(child, relativePath);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void CancelEditingInSubtree(FileEntryViewModel node)
    {
        if (node.IsEditing)
        {
            node.IsEditing = false;
        }

        foreach (FileEntryViewModel child in node.Children)
        {
            CancelEditingInSubtree(child);
        }
    }

    /// <inheritdoc/>
    public bool IsAncestorOf(string ancestorPath, string descendantPath)
    {
        string normalizedAncestor = ancestorPath.Replace('\\', '/').TrimEnd('/');
        string normalizedDescendant = descendantPath.Replace('\\', '/');

        if (string.IsNullOrEmpty(normalizedAncestor)) return true;

        return normalizedDescendant.StartsWith(normalizedAncestor + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public bool IsValidDropTarget(string sourcePath, FileEntryViewModel? target)
    {
        if (target == null) return false;
        
        // No permitir soltar sobre sí mismo
        if (target.RelativePath == sourcePath) return false;
        
        // No permitir soltar sobre un ancestro del origen
        if (IsAncestorOf(sourcePath, target.RelativePath)) return false;

        // Aceptar carpetas, proyecto y ficheros (para estos últimos, el destino será su carpeta padre)
        return target.Type == FileType.Directory 
            || target.Type == FileType.Project 
            || target.Type == FileType.Script 
            || target.Type == FileType.Data 
            || target.Type == FileType.Metadata 
            || target.Type == FileType.Other;
    }
}
