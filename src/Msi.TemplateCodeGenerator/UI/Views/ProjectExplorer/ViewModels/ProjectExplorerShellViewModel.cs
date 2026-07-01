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
internal partial class ProjectExplorerShellViewModel : BaseViewModel, IDisposable
{
    private readonly IProjectContext _projectContext;
    private readonly IProjectService _projectService;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ProjectExplorerShellViewModel> _logger;
    private bool _disposed;

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
        if (value is FileEntryViewModel entry && entry.Type == FileType.Script)
        {
            string absolutePath = Path.Combine(
                _projectContext.CurrentProject!.FolderPath,
                entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));

            _navigationService.OpenFile(absolutePath);
        }
    }

    public ProjectExplorerShellViewModel(
        IProjectContext projectContext,
        IProjectService projectService,
        IMessenger messenger,
        INavigationService navigationService,
        ILogger<ProjectExplorerShellViewModel> logger)
    {
        _projectContext = projectContext;
        _projectService = projectService;
        _messenger = messenger;
        _navigationService = navigationService;
        _logger = logger;

        // Suscribirse a eventos de proyecto
        _messenger.Register<ProjectOpenedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));
        _messenger.Register<ProjectClosedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));
        _messenger.Register<ProjectSavedMessage>(this, (r, m) => ((ProjectExplorerShellViewModel)r).RefreshProjectContextCommand.Execute(null));

        // Inicializar estado
        RefreshProjectContextCommand.Execute(null);
    }

    /// <summary>
    /// Libera los recursos utilizados por el ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _messenger.UnregisterAll(this);
        _logger.LogDebug("ProjectExplorerShellViewModel disposed");
        _disposed = true;
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
        string projectFileName = Path.GetFileName(projectFilePath);

        Dictionary<string, FileEntryViewModel> dict = new(StringComparer.OrdinalIgnoreCase);
        List<FileEntryViewModel> roots = new();

        // Ordenar globalmente: directorios (0) antes que ficheros (1), después alfabético por ruta.
        // Esto garantiza a la vez que los padres se procesan antes que sus hijos
        // y que dentro de cada nodo los directorios aparezcan antes que los ficheros.
        foreach (FileEntry entry in project.Files
            .Where(f => !f.RelativePath.Replace('\\', '/').Equals(projectFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Type == FileType.Directory ? 0 : 1)
            .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            string normalizedPath = entry.RelativePath.Replace('\\', '/');
            FileEntryViewModel vm = new(entry.Name, normalizedPath, entry.Type);
            dict[normalizedPath] = vm;

            int lastSlash = normalizedPath.LastIndexOf('/');
            string? parentPath = lastSlash < 0 ? null : normalizedPath[..lastSlash];

            if (parentPath is not null && dict.TryGetValue(parentPath, out FileEntryViewModel? parent))
                parent.Children.Add(vm);
            else
                roots.Add(vm);
        }

        // Nodo raíz: representa el propio fichero .scribanproj, expandido por defecto
        FileEntryViewModel projectRoot = new(projectFileName, string.Empty, FileType.Project)
        {
            IsExpanded = true
        };
        foreach (FileEntryViewModel root in roots)
            projectRoot.Children.Add(root);

        return new ObservableCollection<FileEntryViewModel> { projectRoot };
    }

    /// <summary>
    /// Comando de prueba: abre un nuevo documento de prueba.
    /// </summary>
    [RelayCommand]
    private async Task CreateTestDocument()
    {
        await _navigationService.OpenFile($"test_{Guid.NewGuid():N}.scriban");
    }

    /// <summary>
    /// Destructor como respaldo por si Dispose no se invoca.
    /// </summary>
    ~ProjectExplorerShellViewModel()
    {
        Dispose();
    }
}
