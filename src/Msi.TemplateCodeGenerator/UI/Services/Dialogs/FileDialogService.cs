using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Views.Shell;

namespace Msi.TemplateCodeGenerator.UI.Services.Dialogs;

/// <summary>
/// Implementación de IFileDialogService usando Avalonia StorageProvider.
/// Encapsula todo acceso al framework de UI para diálogos de archivo del SO.
/// </summary>
internal sealed class FileDialogService : IFileDialogService
{
    private readonly MainWindow _ownerWindow;
    private readonly ILogger<FileDialogService> _logger;

    public FileDialogService(MainWindow ownerWindow, ILogger<FileDialogService> logger)
    {
        _ownerWindow = ownerWindow;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string?> SaveFileAsync(
        string title,
        string defaultExtension,
        string fileTypeName,
        string filePattern,
        string? suggestedFileName = null)
    {
        IReadOnlyList<FilePickerFileType> fileTypeChoices =
        [
            new FilePickerFileType(fileTypeName)
            {
                Patterns = [filePattern]
            }
        ];

        FilePickerSaveOptions options = new()
        {
            Title = title,
            FileTypeChoices = fileTypeChoices,
            DefaultExtension = defaultExtension,
            SuggestedFileName = suggestedFileName
        };

        IStorageFile? file = await _ownerWindow.StorageProvider.SaveFilePickerAsync(options);

        if (file is not null)
        {
            string filePath = file.Path.LocalPath;
            _logger.LogInformation("[UI] FileDialog: Selected '{Path}'", filePath);
            return filePath;
        }

        _logger.LogInformation("[UI] FileDialog: Cancelled");
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> OpenFileAsync(
        string title,
        string fileTypeName,
        string filePattern)
    {
        IReadOnlyList<FilePickerFileType> fileTypeFilter =
        [
            new FilePickerFileType(fileTypeName)
            {
                Patterns = [filePattern]
            }
        ];

        FilePickerOpenOptions options = new()
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypeFilter
        };

        IReadOnlyList<IStorageFile> files = await _ownerWindow.StorageProvider.OpenFilePickerAsync(options);
        IStorageFile? file = files.FirstOrDefault();

        if (file is not null)
        {
            string filePath = file.Path.LocalPath;
            _logger.LogInformation("[UI] FileDialog: Selected '{Path}'", filePath);
            return filePath;
        }

        _logger.LogInformation("[UI] FileDialog: Cancelled");
        return null;
    }
}
