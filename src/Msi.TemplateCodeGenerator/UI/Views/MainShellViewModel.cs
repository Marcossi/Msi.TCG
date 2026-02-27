using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Views;

namespace Msi.TemplateCodeGenerator.UI;

/// <summary>
/// ViewModel principal que coordina el shell y los comandos globales de la aplicación.
/// El layout de paneles se obtiene de INavigationService.
/// Las operaciones de proyecto se delegan en IProjectService.
/// </summary>
internal partial class MainShellViewModel(
    INavigationService navigationService,
    IProjectService projectService)
    : BaseViewModel
{
    private readonly IProjectService _projectService = projectService;

    ///      XAML                    ViewModel
    ///──────────────────────────────────────────────────────────
    /// DockControl  ←──binding──  Layout (IRootDock)
    /// (lo que ves)                (obtenido de INavigationService)

    [ObservableProperty]
    private IRootDock? _layout = navigationService.GetLayout();

    /// <summary>
    /// Crea un nuevo proyecto.
    /// </summary>
    [RelayCommand]
    private async Task NewProjectAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow == null)
            return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Nuevo Proyecto",
            FileTypeChoices =
            [
                new FilePickerFileType(ProjectConstants.ProjectFileTypeName)
                {
                    Patterns = [ProjectConstants.ProjectFilePattern]
                }
            ],
            DefaultExtension = ProjectConstants.ProjectFileExtension,
            SuggestedFileName = "NuevoProyecto"
        });

        if (file != null)
        {
            var filePath = file.Path.LocalPath;
            var projectName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            await _projectService.CreateNewProjectAsync(filePath, projectName);
        }
    }

    /// <summary>
    /// Abre un diálogo para seleccionar un proyecto y lo carga.
    /// </summary>
    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow == null)
            return;

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir Proyecto",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(ProjectConstants.ProjectFileTypeName)
                {
                    Patterns = [ProjectConstants.ProjectFilePattern]
                }
            ]
        });

        var file = files?.FirstOrDefault();
        if (file != null)
            await _projectService.OpenProjectAsync(file.Path.LocalPath);
    }

    /// <summary>
    /// Cierra el proyecto activo.
    /// </summary>
    [RelayCommand]
    private async Task CloseProjectAsync()
    {
        await _projectService.CloseProjectAsync();
    }

    /// <summary>
    /// Guarda el proyecto actual.
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        await _projectService.SaveProjectAsync();
    }

    /// <summary>
    /// Guarda el proyecto en una nueva ubicación.
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow == null)
            return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar Proyecto Como",
            FileTypeChoices =
            [
                new FilePickerFileType(ProjectConstants.ProjectFileTypeName)
                {
                    Patterns = [ProjectConstants.ProjectFilePattern]
                }
            ],
            DefaultExtension = ProjectConstants.ProjectFileExtension
        });

        if (file != null)
            await _projectService.SaveProjectAsAsync(file.Path.LocalPath);
    }

    [RelayCommand]
    private static void Exit()
    {
    }
}
