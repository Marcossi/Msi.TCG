using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IProjectScriptFinder"/> que busca scripts en el árbol de archivos.
/// </summary>
internal sealed class ProjectScriptFinder : IProjectScriptFinder
{
    /// <inheritdoc/>
    public IEnumerable<FileEntryViewModel> FindAllScripts(IEnumerable<FileEntryViewModel> fileTree)
    {
        List<FileEntryViewModel> scripts = new();
        foreach (FileEntryViewModel root in fileTree)
        {
            CollectScripts(root, scripts);
        }
        return scripts;
    }

    private static void CollectScripts(FileEntryViewModel node, List<FileEntryViewModel> scripts)
    {
        if (node.Type == FileType.Script)
            scripts.Add(node);

        foreach (FileEntryViewModel child in node.Children)
        {
            CollectScripts(child, scripts);
        }
    }
}
