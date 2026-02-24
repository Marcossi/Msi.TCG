using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.ProjectExplorer;
using Msi.TemplateCodeGenerator.UI.Settings;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

namespace Msi.TemplateCodeGenerator.UI;

/// <summary>
/// ViewModel principal que coordina la navegación y comandos globales de la aplicación.
/// Gestiona operaciones de proyecto (abrir/cerrar) delegando en IProjectService.
/// </summary>
internal partial class MainShellViewModel(TemplateEditorShellViewModel templateEditorShellViewModel,
                                          SettingsShellViewModel settingsShellViewModel,
                                          ProjectExplorerShellViewModel projectExplorerShellViewModel,
                                          IProjectService projectService)
    : BaseViewModel
{
    [ObservableProperty]
    private object? _currentViewModel = templateEditorShellViewModel;

    [ObservableProperty]
    private object? _currentExplorerViewModel = projectExplorerShellViewModel;

    private readonly IProjectService _projectService = projectService;

    //protected override void OnActivated()
    //{
    //    // Aquí nos suscribiríamos a mensajes globales si los hubiera.
    //    // Messenger.Register<MainShellViewModel, NavigationMessage>(this, (r, m) => r.Receive(m));
    //}

    /// <summary>
    /// Crea un nuevo proyecto.
    /// </summary>
    [RelayCommand]
    private async Task NewProjectAsync()
    {
        // Obtener la ventana principal desde ApplicationLifetime
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow == null)
            return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Nuevo Proyecto",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(ProjectConstants.ProjectFileTypeName)
                {
                    Patterns = new[] { ProjectConstants.ProjectFilePattern }
                }
            },
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
        // Obtener la ventana principal desde ApplicationLifetime
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow == null)
            return;

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir Proyecto",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(ProjectConstants.ProjectFileTypeName)
                {
                    Patterns = new[] { ProjectConstants.ProjectFilePattern }
                }
            }
        });

        var file = files?.FirstOrDefault();
        if (file != null)
        {
            await _projectService.OpenProjectAsync(file.Path.LocalPath);

            // Refrescar el explorador de proyectos
            projectExplorerShellViewModel.RefreshProjectContextCommand.Execute(null);
        }
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
        // Obtener la ventana principal desde ApplicationLifetime
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow == null)
            return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar Proyecto Como",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(ProjectConstants.ProjectFileTypeName)
                {
                    Patterns = new[] { ProjectConstants.ProjectFilePattern }
                }
            },
            DefaultExtension = ProjectConstants.ProjectFileExtension
        });

        if (file != null)
        {
            await _projectService.SaveProjectAsAsync(file.Path.LocalPath);

            // Refrescar el explorador de proyectos
            projectExplorerShellViewModel.RefreshProjectContextCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void NavigateToTemplateEditor()
    {
        CurrentViewModel = templateEditorShellViewModel;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentViewModel = settingsShellViewModel;
    }

    [RelayCommand]
    private static void Exit()
    {
        
    }
}
