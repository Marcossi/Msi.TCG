using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using Microsoft.Extensions.Logging;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IProjectExplorerStateManager"/> que gestiona el estado de UI en memoria.
/// </summary>
internal sealed class ProjectExplorerStateManager(
    IProjectExplorerStateService stateService,
    ILogger<ProjectExplorerStateManager> logger) : IProjectExplorerStateManager
{
    private readonly IProjectExplorerStateService _stateService = stateService;
    private readonly ILogger<ProjectExplorerStateManager> _logger = logger;

    /// <inheritdoc/>
    public HashSet<string> CaptureExpandedPaths(IEnumerable<FileEntryViewModel> fileTree)
    {
        HashSet<string> expanded = new(StringComparer.OrdinalIgnoreCase);
        foreach (FileEntryViewModel root in fileTree)
        {
            CollectExpandedPaths(root, expanded);
        }
        return expanded;
    }

    /// <inheritdoc/>
    public void RestoreExpandedState(IEnumerable<FileEntryViewModel> fileTree, HashSet<string> expandedPaths)
    {
        foreach (FileEntryViewModel root in fileTree)
        {
            RestoreNodeExpandedState(root, expandedPaths);
        }
    }

    /// <inheritdoc/>
    public async Task SaveStateAsync(IEnumerable<FileEntryViewModel> fileTree, string projectPath)
    {
        HashSet<string> expandedPaths = CaptureExpandedPaths(fileTree);
        ProjectExplorerState state = new(expandedPaths.ToList(), null);
        await _stateService.SaveStateAsync(projectPath, state);
    }

    /// <inheritdoc/>
    public async Task RestoreUiStateAsync(IEnumerable<FileEntryViewModel> fileTree, string projectPath)
    {
        ProjectExplorerState? state = await _stateService.LoadStateAsync(projectPath);
        if (state == null) return;

        HashSet<string> savedPaths = new(state.ExpandedPaths, StringComparer.OrdinalIgnoreCase);
        RestoreExpandedState(fileTree, savedPaths);
    }

    private static void CollectExpandedPaths(FileEntryViewModel node, HashSet<string> expanded)
    {
        if (node.IsExpanded)
        {
            expanded.Add(node.RelativePath);
        }

        foreach (FileEntryViewModel child in node.Children)
        {
            CollectExpandedPaths(child, expanded);
        }
    }

    private static void RestoreNodeExpandedState(FileEntryViewModel node, HashSet<string> expandedPaths)
    {
        if (expandedPaths.Contains(node.RelativePath))
        {
            node.IsExpanded = true;
        }

        foreach (FileEntryViewModel child in node.Children)
        {
            RestoreNodeExpandedState(child, expandedPaths);
        }
    }
}
