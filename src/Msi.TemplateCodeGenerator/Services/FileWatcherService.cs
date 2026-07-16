using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Implementación de <see cref="IFileWatcherService"/> basada en <see cref="FileSystemWatcher"/>.
/// Vigila cambios en ficheros .json y .scriban dentro de la carpeta del proyecto.
/// Publica <see cref="ProjectFilesChangedMessage"/> via IMessenger.
/// </summary>
internal sealed class FileWatcherService(
    IMessenger messenger,
    ILogger<FileWatcherService> logger) : IFileWatcherService, IDisposable
{
    private readonly IMessenger _messenger = messenger;
    private readonly ILogger<FileWatcherService> _logger = logger;
    private FileSystemWatcher? _watcher;

    /// <inheritdoc/>
    public void StartWatching(string directoryPath)
    {
        StopWatching();

        _watcher = new FileSystemWatcher(directoryPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;

        _logger.LogInformation("Started watching {Path}", directoryPath);
    }

    /// <inheritdoc/>
    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileCreated;
            _watcher.Deleted -= OnFileDeleted;
            _watcher.Dispose();
            _watcher = null;

            _logger.LogInformation("Stopped watching");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        PublishIfRelevant(e.FullPath, FileChangeType.Changed);
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        PublishIfRelevant(e.FullPath, FileChangeType.Created);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        PublishIfRelevant(e.FullPath, FileChangeType.Deleted);
    }

    private void PublishIfRelevant(string fullPath, FileChangeType changeType)
    {
        if (IsInEditorFolder(fullPath))
        {
            _logger.LogDebug("FileWatcher event ignored (editor folder): {Path}", fullPath);
            return;
        }

        _logger.LogDebug("FileWatcher detected {ChangeType} at {Path}", changeType, fullPath);

        _messenger.Send(new ProjectFilesChangedMessage(fullPath, changeType));
    }

    private static bool IsInEditorFolder(string path)
    {
        string normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Contains($"/{ProjectDirectoryConstants.EditorFolderName}/", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith($"/{ProjectDirectoryConstants.EditorFolderName}", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopWatching();
    }
}
