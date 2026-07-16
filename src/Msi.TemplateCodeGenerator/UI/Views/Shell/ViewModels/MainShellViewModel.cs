using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Controls;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Services.Commands;
using Msi.TemplateCodeGenerator.UI.Shared;

namespace Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;

/// <summary>
/// ViewModel principal que coordina el shell y los comandos globales de la aplicación.
/// El layout de paneles se obtiene de INavigationService.
/// Las operaciones de proyecto se delegan en IProjectService.
/// </summary>
internal partial class MainShellViewModel : BaseViewModel, IDisposable
{
    private readonly IProjectService _projectService;
    private readonly IProjectContext _projectContext;
    private readonly IFileDialogService _fileDialogService;
    private readonly ICommandRegistry _commandRegistry;
    private readonly ITemplatesService _templatesService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IApp _app;
    private readonly IMessenger _messenger;
    private readonly ILogger<MainShellViewModel> _logger;
    private bool _disposed;

    ///      XAML                    ViewModel
    ///──────────────────────────────────────────────────────────
    /// DockControl  ←──binding──  Layout (IRootDock)
    /// (lo que ves)                (obtenido de INavigationService)

    [ObservableProperty]
    private IRootDock? _layout;

    /// <summary>
    /// Mensaje de estado para feedback al usuario en la barra de estado.
    /// </summary>
    [ObservableProperty]
    private string? _statusMessage;

    public MainShellViewModel(
        INavigationService navigationService,
        IProjectService projectService,
        IProjectContext projectContext,
        IFileDialogService fileDialogService,
        ICommandRegistry commandRegistry,
        ITemplatesService templatesService,
        IDialogService dialogService,
        IApp app,
        IMessenger messenger,
        ILogger<MainShellViewModel> logger)
    {
        _navigationService = navigationService;
        _projectService = projectService;
        _projectContext = projectContext;
        _fileDialogService = fileDialogService;
        _commandRegistry = commandRegistry;
        _templatesService = templatesService;
        _dialogService = dialogService;
        _app = app;
        _messenger = messenger;
        _logger = logger;
        _layout = navigationService.GetLayout();

        _messenger.Register<ProjectOpenedMessage>(this, (r, m) =>
            ((MainShellViewModel)r).NotifyCanExecuteChangedForProjectCommands());
        _messenger.Register<ProjectClosedMessage>(this, (r, m) =>
            ((MainShellViewModel)r).NotifyCanExecuteChangedForProjectCommands());
    }

    /// <summary>
    /// Crea un nuevo proyecto.
    /// </summary>
    [RelayCommand]
    private async Task NewProjectAsync()
    {
        _logger.LogInformation("[UI] Command: NewProject");
        try
        {
            string? filePath = await _fileDialogService.SaveFileAsync(
                "Nuevo Proyecto",
                ProjectConstants.ProjectFileExtension,
                ProjectConstants.ProjectFileTypeName,
                ProjectConstants.ProjectFilePattern,
                "NuevoProyecto");

            if (filePath is null)
                return;

            string projectName = Path.GetFileNameWithoutExtension(filePath);
            await _projectService.CreateNewProjectAsync(filePath, projectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing NewProject");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Abre un diálogo para seleccionar un proyecto y lo carga.
    /// </summary>
    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        _logger.LogInformation("[UI] Command: OpenProject");
        try
        {
            string? filePath = await _fileDialogService.OpenFileAsync(
                "Abrir Proyecto",
                ProjectConstants.ProjectFileTypeName,
                ProjectConstants.ProjectFilePattern);

            if (filePath is null)
                return;

            await _projectService.OpenProjectAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing OpenProject");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Cierra el proyecto activo.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCloseProject))]
    private async Task CloseProjectAsync()
    {
        _logger.LogInformation("[UI] Command: CloseProject");
        try
        {
            await _projectService.CloseProjectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing CloseProject");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanCloseProject() => _projectContext.IsProjectOpen;

    /// <summary>
    /// Guarda el proyecto actual.
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        _logger.LogInformation("[UI] Command: SaveProject");
        try
        {
            await _projectService.SaveProjectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing SaveProject");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Guarda el proyecto en una nueva ubicación.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveProjectAs))]
    private async Task SaveProjectAsAsync()
    {
        _logger.LogInformation("[UI] Command: SaveProjectAs");
        try
        {
            string? filePath = await _fileDialogService.SaveFileAsync(
                "Guardar Proyecto Como",
                ProjectConstants.ProjectFileExtension,
                ProjectConstants.ProjectFileTypeName,
                ProjectConstants.ProjectFilePattern);

            if (filePath is null)
                return;

            await _projectService.SaveProjectAsAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing SaveProjectAs");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanSaveProjectAs() => _projectContext.IsProjectOpen;

    /// <summary>
    /// Cierra la aplicación de forma controlada.
    /// Consulta CanCloseAllAsync() antes de cerrar para permitir guardar cambios pendientes.
    /// </summary>
    [RelayCommand]
    private async Task ExitAsync()
    {
        _logger.LogInformation("[UI] Command: Exit");
        try
        {
            bool canClose = await _navigationService.CanCloseAllAsync();
            if (canClose)
            {
                _logger.LogInformation("[UI] Cierre de aplicación confirmado");
                _app.Shutdown();
            }
            else
            {
                _logger.LogInformation("[UI] Cierre de aplicación cancelado por el usuario");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing Exit");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Guarda el contexto activo (editor de archivos).
    /// Delega en ICommandRegistry para resolver el comando contextual.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        _logger.LogInformation("[UI] Command: Save (contextual)");
        try
        {
            bool executed = await _commandRegistry.ExecuteAsync("Save");
            if (!executed)
            {
                _logger.LogDebug("No se pudo ejecutar Save contextual (sin editor activo o sin cambios)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing Save (contextual)");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanSave() => _commandRegistry.CanExecute("Save");

    /// <summary>
    /// Ejecuta el script del editor activo.
    /// Delega en ICommandRegistry para resolver el comando contextual.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGenerateCurrent))]
    private async Task GenerateCurrentAsync()
    {
        _logger.LogInformation("[UI] Command: GenerateCurrent (contextual)");
        try
        {
            bool executed = await _commandRegistry.ExecuteAsync("Generate");
            if (!executed)
            {
                _logger.LogDebug("No se pudo ejecutar Generate contextual (sin editor activo)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing GenerateCurrent (contextual)");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanGenerateCurrent() => _commandRegistry.CanExecute("Generate");

    /// <summary>
    /// Ejecuta todos los scripts del proyecto abierto.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGenerateAll))]
    private async Task GenerateAllAsync()
    {
        _logger.LogInformation("[UI] Command: GenerateAll");
        try
        {
            BatchExecutionResult result = await _templatesService.ExecuteAllScriptsAsync();

            string message = $"Generated {result.SuccessCount} script(s)";
            if (result.ErrorCount > 0)
            {
                message += $"\n\n{result.ErrorCount} script(s) failed:\n{string.Join("\n", result.Errors)}";
            }

            await _dialogService.ShowInfoAsync(message, "Generate All Complete");

            _logger.LogInformation("GenerateAll completed: {Success} success, {Error} errors",
                result.SuccessCount, result.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing GenerateAll");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanGenerateAll() => _projectContext.IsProjectOpen;

    /// <summary>
    /// Notifica a la UI que el estado de CanExecute ha cambiado para los comandos
    /// que dependen del estado del proyecto.
    /// </summary>
    private void NotifyCanExecuteChangedForProjectCommands()
    {
        CloseProjectCommand.NotifyCanExecuteChanged();
        SaveProjectAsCommand.NotifyCanExecuteChanged();
        GenerateAllCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Libera los recursos utilizados por el ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _messenger.UnregisterAll(this);
        _logger.LogDebug("MainShellViewModel disposed");
        _disposed = true;
    }

    /// <summary>
    /// Destructor como respaldo por si Dispose no se invoca.
    /// </summary>
    ~MainShellViewModel()
    {
        Dispose();
    }
}
