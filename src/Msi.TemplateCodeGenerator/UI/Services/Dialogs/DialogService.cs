using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.Shell;

namespace Msi.TemplateCodeGenerator.UI.Services.Dialogs;

/// <summary>
/// Implementación de IDialogService usando Avalonia.
/// Muestra diálogos de confirmación y otras interacciones con el usuario.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private readonly MainWindow _ownerWindow;
    private readonly ILogger<DialogService> _logger;

    public DialogService(MainWindow ownerWindow, ILogger<DialogService> logger)
    {
        _ownerWindow = ownerWindow;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SaveConfirmationResult> ShowSaveConfirmationAsync(string fileName)
    {
        _logger.LogInformation("[UI] DialogService: Showing save confirmation for '{FileName}'", fileName);
        SaveConfirmationDialog dialog = new(fileName);
        await dialog.ShowDialog(_ownerWindow);
        _logger.LogInformation("[UI] DialogService: Result = {Result}", dialog.Result);
        return dialog.Result;
    }
}
