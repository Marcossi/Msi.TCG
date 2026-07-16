using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Shared;

namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

/// <summary>
/// ViewModel del explorador de proyectos que refleja el estado del contexto actual.
/// Se suscribe a mensajes de cambios de proyecto para actualizar automáticamente la UI.
/// </summary>
internal partial class ProjectExplorerShellViewModel : BaseViewModel, IDisposable, IProjectExplorerCommands
{
    private readonly IProjectContext _projectContext;
    private readonly IProjectService _projectService;
    private readonly IElementCatalog _elementCatalog;
    private readonly IFileSystem _fileSystem;
    private readonly IScriptEngine _scriptEngine;
    private readonly IProjectTreeBuilder _treeBuilder;
    private readonly IProjectScriptFinder _scriptFinder;
    private readonly IProjectFileOperations _fileOperations;
    private readonly IInlineEditingService _inlineEditing;
    private readonly IProjectExplorerStateManager _stateManager;
    private readonly IFileWatcherService _fileWatcher;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IProjectExplorerStateService _stateService;
    private readonly ILogger<ProjectExplorerShellViewModel> _logger;
    private bool _disposed;
    private CancellationTokenSource? _saveStateCts;
    private string? _pendingEditRelativePath;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private string _projectName = "sin solución";

    [ObservableProperty]
    private ObservableCollection<FileEntryViewModel> _fileTree = new();

    [ObservableProperty]
    private object? _selectedFileEntry;

    partial void OnSelectedFileEntryChanged(object? value)
    {
        if (value is FileEntryViewModel entry)
        {
            _logger.LogInformation("[UI] FileEntry seleccionado: '{Name}' (Type={Type})",
                entry.Name, entry.Type);
        }

        ScheduleSaveState();
    }

    public ProjectExplorerShellViewModel(
        IProjectContext projectContext,
        IProjectService projectService,
        IElementCatalog elementCatalog,
        IFileSystem fileSystem,
        IScriptEngine scriptEngine,
        IProjectTreeBuilder treeBuilder,
        IProjectScriptFinder scriptFinder,
        IProjectFileOperations fileOperations,
        IInlineEditingService inlineEditing,
        IProjectExplorerStateManager stateManager,
        IFileWatcherService fileWatcher,
        IMessenger messenger,
        INavigationService navigationService,
        IDialogService dialogService,
        IProjectExplorerStateService stateService,
        ILogger<ProjectExplorerShellViewModel> logger)
    {
        _projectContext = projectContext;
        _projectService = projectService;
        _elementCatalog = elementCatalog;
        _fileSystem = fileSystem;
        _scriptEngine = scriptEngine;
        _treeBuilder = treeBuilder;
        _scriptFinder = scriptFinder;
        _fileOperations = fileOperations;
        _inlineEditing = inlineEditing;
        _stateManager = stateManager;
        _fileWatcher = fileWatcher;
        _messenger = messenger;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _stateService = stateService;
        _logger = logger;

        _messenger.Register<ProjectOpenedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));
        _messenger.Register<ProjectClosedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));
        _messenger.Register<ProjectFilesChangedMessage>(this, async (r, m) => await ((ProjectExplorerShellViewModel)r).HandleProjectFilesChanged(m));

        RefreshProjectContextCommand.Execute(null);
    }

    /// <summary>
    /// Handler de cambios de fichero detectados por FileWatcher o operaciones del proyecto.
    /// Filtra por extensión relevante y aplica debounce antes de refrescar el árbol.
    /// </summary>
    private async Task HandleProjectFilesChanged(ProjectFilesChangedMessage message)
    {
        // Si hay payload específico, filtrar por extensión relevante
        if (message.RelativePath is not null)
        {
            string extension = Path.GetExtension(message.RelativePath).ToLowerInvariant();
            if (extension != ".json" && extension != ".scriban")
                return;

            _logger.LogInformation("File changed: {Path} ({ChangeType})", message.RelativePath, message.ChangeType);
        }

        // Debounce de 500ms para evitar múltiples refreshes rápidos
        await Task.Delay(500);
        await RefreshProjectContext();
    }

    /// <summary>
    /// Libera los recursos utilizados por el ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _messenger.UnregisterAll(this);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error durante Dispose de ProjectExplorerShellViewModel");
        }

        _saveStateCts?.Cancel();
        _saveStateCts?.Dispose();
        _logger.LogDebug("ProjectExplorerShellViewModel disposed");
    }

    /// <summary>
    /// Marca los ficheros con errores de carga (JSONs inválidos) o de sintaxis (scripts inválidos).
    /// </summary>
    private async Task MarkLoadErrorsAsync()
    {
        IReadOnlyList<LoadError> loadErrors = _elementCatalog.GetLoadErrors();

        foreach (LoadError error in loadErrors)
        {
            FileEntryViewModel? fileEntry = FindFileEntryByAbsolutePath(error.FilePath);
            fileEntry?.SetError(error.Message);
        }

        foreach (FileEntryViewModel scriptEntry in _scriptFinder.FindAllScripts(FileTree))
        {
            try
            {
                string absolutePath = Path.Combine(
                    _projectContext.CurrentProject!.FolderPath,
                    scriptEntry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                string content = await _fileSystem.ReadTextAsync(absolutePath);
                IReadOnlyList<string> syntaxErrors = await _scriptEngine.ValidateSyntaxAsync(content);

                if (syntaxErrors.Count > 0)
                {
                    scriptEntry.SetError(string.Join(", ", syntaxErrors));
                }
            }
            catch (Exception ex)
            {
                scriptEntry.SetError(ex.Message);
            }
        }
    }

    /// <summary>
    /// Busca una entrada de fichero en el árbol por su ruta absoluta.
    /// Convierte la ruta absoluta a relativa y delega en IProjectFileOperations.
    /// </summary>
    private FileEntryViewModel? FindFileEntryByAbsolutePath(string absolutePath)
    {
        string projectPath = _projectContext.CurrentProject?.FolderPath ?? string.Empty;
        string relativePath = Path.GetRelativePath(projectPath, absolutePath).Replace('\\', '/');
        return _fileOperations.FindFileEntryByRelativePath(FileTree, relativePath);
    }

    /// <summary>
    /// Refresca el estado del explorador: rescanea el disco y reconstruye el árbol.
    /// Preserva el estado de expansión de las carpetas y la selección del fichero activo
    /// capturando los paths antes de reconstruir y restaurándolos después.
    /// Tras la reconstrucción, restaura el estado de UI persistido (carpetas abiertas y documento activo).
    /// </summary>
    [RelayCommand]
    private async Task RefreshProjectContext()
    {
        IsProjectOpen = _projectContext.IsProjectOpen;

        if (!IsProjectOpen)
        {
            ProjectName = "sin solución";
            FileTree = new ObservableCollection<FileEntryViewModel>();
            SelectedFileEntry = null;
            return;
        }

        ProjectName = _projectContext.CurrentProject?.Name ?? "sin solución";

        HashSet<string> expandedPaths = _stateManager.CaptureExpandedPaths(FileTree);
        string? selectedPath = (SelectedFileEntry as FileEntryViewModel)?.RelativePath;

        await _projectService.RefreshFilesAsync();
        await _elementCatalog.ReloadAsync();
        FileTree = _treeBuilder.BuildFileTree(_projectContext.CurrentProject!, _projectContext.CurrentProjectPath!, expandedPaths);
        await MarkLoadErrorsAsync();
        await RestoreUiStateAsync();

        if (selectedPath is not null)
        {
            FileEntryViewModel? found = _fileOperations.FindFileEntryByRelativePath(FileTree, selectedPath);
            if (found is not null)
            {
                SelectedFileEntry = found;
            }
        }

        if (_pendingEditRelativePath is not null)
        {
            string pendingPath = _pendingEditRelativePath;
            _pendingEditRelativePath = null;

            FileEntryViewModel? found = _fileOperations.FindFileEntryByRelativePath(FileTree, pendingPath);
            if (found is not null)
            {
                found.EditingName = found.Name;
                found.IsEditing = true;
                SelectedFileEntry = found;
            }
        }
    }

    /// <summary>
    /// Abre el fichero representado por la entrada en el editor correspondiente.
    /// Invocado por doble-click en el TreeView. Delega la resolución del editor en NavigationService.
    /// </summary>
    [RelayCommand]
    private async Task OpenFile(FileEntryViewModel? entry)
    {
        if (entry == null)
        {
            _logger.LogDebug("OpenFile invocado con entry null");
            return;
        }

        _logger.LogInformation("[UI] Command: OpenFile '{Name}' (Type={Type}, Path={Path})",
            entry.Name, entry.Type, entry.RelativePath);

        if (entry.Type == FileType.Directory || entry.Type == FileType.Project)
        {
            _logger.LogDebug("Item no editable ignorado: '{Name}' (Type={Type})", entry.Name, entry.Type);
            return;
        }

        string absolutePath = Path.Combine(
            _projectContext.CurrentProject!.FolderPath,
            entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        _logger.LogDebug("Invocando NavigationService.OpenFile('{AbsolutePath}')", absolutePath);
        await _navigationService.OpenFile(absolutePath);
        _logger.LogInformation("[UI] Archivo abierto en editor: '{Path}'", entry.RelativePath);
    }

    /// <summary>
    /// Crea un nuevo fichero en disco con nombre por defecto y lo pone en modo edición inline.
    /// Si parent es null o es un fichero, se usa la raíz del proyecto.
    /// </summary>
    [RelayCommand]
    private async Task CreateFile(FileEntryViewModel? parent)
    {
        if (!_projectContext.IsProjectOpen) return;

        string parentPath = _fileOperations.ResolveParentRelativePath(parent);

        _logger.LogInformation("[UI] Command: CreateFile (parent={Parent})", parent?.Name ?? "root");

        try
        {
            FileEntry newEntry = await _projectService.CreateFileAsync(parentPath, "NuevoFichero.scriban");
            _pendingEditRelativePath = newEntry.RelativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear fichero");
            await _dialogService.ShowErrorAsync($"Error al crear fichero: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Crea un nuevo directorio en disco con nombre por defecto y lo pone en modo edición inline.
    /// Si parent es null o es un fichero, se usa la raíz del proyecto.
    /// </summary>
    [RelayCommand]
    private async Task CreateDirectory(FileEntryViewModel? parent)
    {
        if (!_projectContext.IsProjectOpen) return;

        string parentPath = _fileOperations.ResolveParentRelativePath(parent);

        _logger.LogInformation("[UI] Command: CreateDirectory (parent={Parent})", parent?.Name ?? "root");

        try
        {
            FileEntry newEntry = await _projectService.CreateDirectoryAsync(parentPath, "NuevaCarpeta");
            _pendingEditRelativePath = newEntry.RelativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear directorio");
            await _dialogService.ShowErrorAsync($"Error al crear carpeta: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Inicia el modo de edición inline para renombrar la entrada indicada.
    /// </summary>
    [RelayCommand]
    private void Rename(FileEntryViewModel? entry)
    {
        _inlineEditing.StartRename(entry, FileTree);
    }

    /// <summary>
    /// Confirma la edición inline: renombra el elemento si el nombre ha cambiado.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmRename(FileEntryViewModel? entry)
    {
        await _inlineEditing.ConfirmRenameAsync(entry, _projectService);
    }

    /// <summary>
    /// Cancela la edición inline.
    /// </summary>
    [RelayCommand]
    private void CancelRename(FileEntryViewModel? entry)
    {
        _inlineEditing.CancelRename(entry);
    }

    /// <summary>
    /// Elimina un fichero o directorio del proyecto tras confirmación del usuario.
    /// </summary>
    [RelayCommand]
    private async Task Delete(FileEntryViewModel? entry)
    {
        if (entry == null || entry.Type == FileType.Project) return;

        _logger.LogInformation("[UI] Command: Delete '{Name}'", entry.Name);

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            $"¿Eliminar '{entry.Name}'? Esta acción no se puede deshacer.",
            "Confirmar eliminación");

        if (!confirmed) return;

        try
        {
            await _projectService.DeleteAsync(entry.RelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar '{Name}'", entry.Name);
            await _dialogService.ShowErrorAsync($"Error al eliminar: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Duplica un fichero o directorio del proyecto.
    /// </summary>
    [RelayCommand]
    private async Task Duplicate(FileEntryViewModel? entry)
    {
        if (entry == null || entry.Type == FileType.Project) return;

        _logger.LogInformation("[UI] Command: Duplicate '{Name}'", entry.Name);

        try
        {
            await _projectService.DuplicateAsync(entry.RelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al duplicar '{Name}'", entry.Name);
            await _dialogService.ShowErrorAsync($"Error al duplicar: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Mueve un fichero o directorio al directorio destino indicado.
    /// Invocado desde el Drag &amp; Drop en la View.
    /// Si el destino es un fichero, se mueve a la carpeta padre de ese fichero.
    /// Si el destino es la misma carpeta donde ya está el origen, no hace nada.
    /// </summary>
    [RelayCommand]
    private async Task Move((string SourcePath, FileEntryViewModel? Target) parameter)
    {
        if (string.IsNullOrEmpty(parameter.SourcePath) || parameter.Target == null) return;

        FileEntryViewModel target = parameter.Target;
        
        // Si el destino es un fichero, usar su carpeta padre como destino real
        string targetParentPath = target.Type == FileType.Directory || target.Type == FileType.Project
            ? target.RelativePath
            : Path.GetDirectoryName(target.RelativePath)?.Replace('\\', '/') ?? string.Empty;

        // Calcular la carpeta padre del origen
        string sourceParentPath = Path.GetDirectoryName(parameter.SourcePath)?.Replace('\\', '/') ?? string.Empty;

        // Si el destino es la misma carpeta donde ya está el origen, no hacer nada
        if (string.Equals(sourceParentPath, targetParentPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Move: Source '{Source}' is already in target folder '{Target}'", 
                parameter.SourcePath, targetParentPath);
            return;
        }

        _logger.LogInformation("[UI] Command: Move '{Source}' → '{Target}'",
            parameter.SourcePath, targetParentPath);

        try
        {
            await _projectService.MoveAsync(parameter.SourcePath, targetParentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al mover '{Source}'", parameter.SourcePath);
            await _dialogService.ShowErrorAsync($"Error al mover: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Destructor como respaldo por si Dispose no se invoca.
    /// </summary>
    ~ProjectExplorerShellViewModel()
    {
        Dispose();
    }

    private void ScheduleSaveState()
    {
        if (!_projectContext.IsProjectOpen) return;

        _saveStateCts?.Cancel();
        _saveStateCts = new CancellationTokenSource();
        CancellationToken token = _saveStateCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                await _stateManager.SaveStateAsync(FileTree, _projectContext.CurrentProjectPath!);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar estado del explorador");
            }
        });
    }

    private async Task RestoreUiStateAsync()
    {
        if (!_projectContext.IsProjectOpen || string.IsNullOrEmpty(_projectContext.CurrentProjectPath))
            return;

        try
        {
            await _stateManager.RestoreUiStateAsync(FileTree, _projectContext.CurrentProjectPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al restaurar estado del explorador");
        }
    }

    #region IProjectExplorerCommands

    /// <inheritdoc/>
    public void ExecuteCreateFile(FileEntryViewModel? parent) => _ = CreateFile(parent);

    /// <inheritdoc/>
    public void ExecuteCreateDirectory(FileEntryViewModel? parent) => _ = CreateDirectory(parent);

    /// <inheritdoc/>
    public void ExecuteRename(FileEntryViewModel? entry) => Rename(entry);

    /// <inheritdoc/>
    public void ExecuteDelete(FileEntryViewModel? entry) => _ = Delete(entry);

    /// <inheritdoc/>
    public void ExecuteDuplicate(FileEntryViewModel? entry) => _ = Duplicate(entry);

    /// <inheritdoc/>
    public void ExecuteRefresh() => _ = RefreshProjectContext();

    #endregion
}
