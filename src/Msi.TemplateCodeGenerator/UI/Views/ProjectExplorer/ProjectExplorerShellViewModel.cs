using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views;

namespace Msi.TemplateCodeGenerator.UI.ProjectExplorer;

/// <summary>
/// ViewModel del explorador de proyectos que refleja el estado del contexto actual.
/// Se suscribe a mensajes de cambios de proyecto para actualizar automáticamente la UI.
/// </summary>
internal partial class ProjectExplorerShellViewModel : BaseViewModel
{
    private readonly IProjectContext _projectContext;
    private readonly IProjectService _projectService;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private string _projectName = "sin solución";

    [ObservableProperty]
    private ObservableCollection<FileEntryViewModel> _fileTree = new();

    public ProjectExplorerShellViewModel(
        IProjectContext projectContext,
        IProjectService projectService,
        IMessenger messenger,
        INavigationService navigationService)
    {
        _projectContext = projectContext;
        _projectService = projectService;
        _messenger = messenger;
        _navigationService = navigationService;

        // Suscribirse a eventos de proyecto
        _messenger.Register<ProjectOpenedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));
        _messenger.Register<ProjectClosedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));
        _messenger.Register<ProjectSavedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));

        // Inicializar estado
        RefreshProjectContextCommand.Execute(null);
    }

    /// <summary>
    /// Refresca el estado del explorador: rescanea el disco y reconstruye el árbol.
    /// </summary>
    [RelayCommand]
    private async Task RefreshProjectContext()
    {
        IsProjectOpen = _projectContext.IsProjectOpen;

        if (!IsProjectOpen)
        {
            ProjectName = "sin solución";
            FileTree = new ObservableCollection<FileEntryViewModel>();
            return;
        }

        ProjectName = _projectContext.CurrentProject?.Name ?? "sin solución";

        await _projectService.RefreshFilesAsync();
        FileTree = BuildFileTree(_projectContext.CurrentProject!, _projectContext.CurrentProjectPath!);
    }

    /// <summary>
    /// Construye el árbol de <see cref="FileEntryViewModel"/> a partir de la lista plana del modelo de dominio.
    /// </summary>
    private static ObservableCollection<FileEntryViewModel> BuildFileTree(Project project, string projectFilePath)
    {
        var projectFileName = Path.GetFileName(projectFilePath);

        var dict = new Dictionary<string, FileEntryViewModel>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<FileEntryViewModel>();

        // Ordenar globalmente: directorios (0) antes que ficheros (1), después alfabético por ruta.
        // Esto garantiza a la vez que los padres se procesan antes que sus hijos
        // y que dentro de cada nodo los directorios aparezcan antes que los ficheros.
        foreach (var entry in project.Files
            .Where(f => !f.RelativePath.Replace('\\', '/').Equals(projectFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Type == FileType.Directory ? 0 : 1)
            .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            var normalizedPath = entry.RelativePath.Replace('\\', '/');
            var vm = new FileEntryViewModel(entry.Name, normalizedPath, entry.Type);
            dict[normalizedPath] = vm;

            var lastSlash = normalizedPath.LastIndexOf('/');
            var parentPath = lastSlash < 0 ? null : normalizedPath[..lastSlash];

            if (parentPath is not null && dict.TryGetValue(parentPath, out var parent))
                parent.Children.Add(vm);
            else
                roots.Add(vm);
        }

        // Nodo raíz: representa el propio fichero .scribanproj, expandido por defecto
        var projectRoot = new FileEntryViewModel(projectFileName, string.Empty, FileType.Project)
        {
            IsExpanded = true
        };
        foreach (var root in roots)
            projectRoot.Children.Add(root);

        return new ObservableCollection<FileEntryViewModel> { projectRoot };
    }

    /// <summary>
    /// Comando de prueba: abre un nuevo documento de prueba.
    /// </summary>
    [RelayCommand]
    private void CreateTestDocument()
    {
        _navigationService.OpenFile($"test_{Guid.NewGuid():N}.scriban");
    }

    /// <summary>
    /// Abre el fichero de la entrada seleccionada en una nueva pestaña del editor.
    /// Solo actúa sobre entradas de tipo <see cref="FileType.Script"/>.
    /// </summary>
    [RelayCommand]
    private void OpenEntry(FileEntryViewModel? entry)
    {
        if (entry?.Type != FileType.Script) return;

        var absolutePath = Path.Combine(
            _projectContext.CurrentProject!.FolderPath,
            entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        _navigationService.OpenFile(absolutePath);
    }
}
