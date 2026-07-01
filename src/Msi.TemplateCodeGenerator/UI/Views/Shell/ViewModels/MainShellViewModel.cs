using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Shared;

namespace Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;

/// <summary>
/// ViewModel principal que coordina el shell y los comandos globales de la aplicación.
/// El layout de paneles se obtiene de INavigationService.
/// Las operaciones de proyecto se delegan en IProjectService.
/// </summary>
internal partial class MainShellViewModel : BaseViewModel
{
    private readonly IProjectService _projectService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ILogger<MainShellViewModel> _logger;

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
        IFileDialogService fileDialogService,
        ILogger<MainShellViewModel> logger)
    {
        _projectService = projectService;
        _fileDialogService = fileDialogService;
        _logger = logger;
        _layout = navigationService.GetLayout();
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

            string projectName = System.IO.Path.GetFileNameWithoutExtension(filePath);
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
    [RelayCommand]
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
    [RelayCommand]
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

    /// <summary>
    /// Cierra la aplicación.
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        _logger.LogInformation("[UI] Command: Exit");
    }
}
