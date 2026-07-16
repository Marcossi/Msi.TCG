using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using Microsoft.Extensions.Logging;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IInlineEditingService"/> para manejar la edición inline de nombres.
/// </summary>
internal sealed class InlineEditingService(
    IProjectFileOperations fileOperations,
    IDialogService dialogService,
    ILogger<InlineEditingService> logger) : IInlineEditingService
{
    private readonly IProjectFileOperations _fileOperations = fileOperations;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILogger<InlineEditingService> _logger = logger;

    /// <inheritdoc/>
    public void StartRename(FileEntryViewModel? entry, IEnumerable<FileEntryViewModel> fileTree)
    {
        if (entry == null || entry.Type == FileType.Project) return;

        _logger.LogInformation("[UI] Command: Rename '{Name}'", entry.Name);

        // Cancelar cualquier edición activa en otros nodos
        _fileOperations.CancelAllEditing(fileTree);

        entry.EditingName = entry.Name;
        entry.IsEditing = true;
    }

    /// <inheritdoc/>
    public async Task<bool> ConfirmRenameAsync(FileEntryViewModel? entry, IProjectService projectService)
    {
        if (entry == null || !entry.IsEditing) return false;

        string newName = entry.EditingName.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            await _dialogService.ShowWarningAsync("El nombre no puede estar vacío.", "Nombre no válido");
            return false;
        }

        entry.IsEditing = false;

        if (newName != entry.Name)
        {
            return await ExecuteRenameAsync(entry, newName, projectService);
        }

        return true;
    }

    /// <inheritdoc/>
    public void CancelRename(FileEntryViewModel? entry)
    {
        if (entry == null) return;

        entry.IsEditing = false;
    }

    private async Task<bool> ExecuteRenameAsync(FileEntryViewModel entry, string newName, IProjectService projectService)
    {
        try
        {
            await projectService.RenameAsync(entry.RelativePath, newName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al renombrar '{Name}'", entry.Name);
            await _dialogService.ShowErrorAsync($"Error al renombrar: {ex.Message}", "Error");
            entry.IsEditing = false;
            return false;
        }
    }
}
