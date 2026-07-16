using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Servicio que gestiona las operaciones relacionadas con proyectos.
/// Actualiza el IProjectContext y maneja FileWatcher, carga/guardado, validaciones, etc.
/// Notifica cambios mediante mensajería (IMessenger).
/// </summary>
internal sealed partial class ProjectService(
    IProjectContext context,
    IProjectContextMutator mutator,
    IProjectSerializer serializer,
    IElementCatalog elementCatalog,
    IFileWatcherService fileWatcher,
    IFileSystem fileSystem,
    IProjectExplorerStateService projectExplorerStateService,
    IMessenger messenger,
    ILogger<ProjectService> logger) : IProjectService
{
    private readonly ILogger<ProjectService> _logger = logger;
    private readonly IProjectContext _context = context;
    private readonly IProjectContextMutator _mutator = mutator;
    private readonly IProjectSerializer _serializer = serializer;
    private readonly IElementCatalog _elementCatalog = elementCatalog;
    private readonly IFileWatcherService _fileWatcher = fileWatcher;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IProjectExplorerStateService _projectExplorerStateService = projectExplorerStateService;
    private readonly IMessenger _messenger = messenger;

    /// <summary>
    /// Abre un proyecto desde la ruta especificada.
    /// </summary>
    public async Task OpenProjectAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
        }

        _logger.LogInformation("Abriendo proyecto desde '{ProjectPath}'", projectPath);

        // Cargar proyecto desde disco usando el serializador
        Models.Project project = await _serializer.LoadAsync(projectPath);

        project.FolderPath = Path.GetDirectoryName(projectPath) ?? string.Empty;

        _mutator.SetProject(project, projectPath);

        // Recargar catálogo de Elements
        await _elementCatalog.ReloadAsync();

        // Crear estructura de carpetas del editor
        await _projectExplorerStateService.EnsureEditorDirectoriesExistAsync(projectPath);

        // Iniciar FileWatcher
        _fileWatcher.StartWatching(project.FolderPath);

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectOpenedMessage(projectPath));

        _logger.LogInformation("Proyecto abierto: '{ProjectPath}'", projectPath);
    }

    /// <summary>
    /// Cierra el proyecto activo.
    /// </summary>
    public Task CloseProjectAsync()
    {
        _logger.LogInformation("Cerrando proyecto activo");

        // Detener FileWatcher
        _fileWatcher.StopWatching();

        _mutator.ClearProject();

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectClosedMessage());

        _logger.LogInformation("Proyecto cerrado");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Guarda el proyecto actual en disco.
    /// </summary>
    public async Task SaveProjectAsync()
    {
        if (!_context.IsProjectOpen)
        {
            throw new InvalidOperationException("No project is currently open.");
        }

        if (string.IsNullOrWhiteSpace(_context.CurrentProjectPath))
        {
            throw new InvalidOperationException("Project path is not set.");
        }

        string path = _context.CurrentProjectPath;
        _logger.LogInformation("Guardando proyecto en '{ProjectPath}'", path);

        await _serializer.SaveAsync(_context.CurrentProject!, path);

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectSavedMessage(path));

        _logger.LogInformation("Proyecto guardado en '{ProjectPath}'", path);
    }

    /// <summary>
    /// Guarda el proyecto actual en una nueva ubicación.
    /// </summary>
    public async Task SaveProjectAsAsync(string newProjectPath)
    {
        if (!_context.IsProjectOpen)
        {
            throw new InvalidOperationException("No project is currently open.");
        }

        if (string.IsNullOrWhiteSpace(newProjectPath))
        {
            throw new ArgumentException("Project path cannot be empty.", nameof(newProjectPath));
        }

        _logger.LogInformation("Guardando proyecto como en '{ProjectPath}'", newProjectPath);

        await _serializer.SaveAsync(_context.CurrentProject!, newProjectPath);

        // Actualizar la ruta actual después de guardar
        _mutator.UpdateProjectPath(newProjectPath);

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectSavedMessage(newProjectPath));

        _logger.LogInformation("Proyecto guardado como en '{ProjectPath}'", newProjectPath);
    }

    /// <summary>
    /// Crea un nuevo proyecto en la ruta especificada.
    /// </summary>
    public async Task CreateNewProjectAsync(string projectPath, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(projectName));
        }

        _logger.LogInformation("Creando nuevo proyecto '{ProjectName}' en '{ProjectPath}'", projectName, projectPath);

        // Crear nuevo proyecto
        Models.Project project = new()
        {
            Name = projectName,
            FolderPath = Path.GetDirectoryName(projectPath) ?? string.Empty
        };

        // Guardar el proyecto en disco
        await _serializer.SaveAsync(project, projectPath);

        // TODO: Crear estructura de carpetas adicionales si es necesario

        // Actualizar el contexto
        _mutator.SetProject(project, projectPath);

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectOpenedMessage(projectPath));

        _logger.LogInformation("Nuevo proyecto creado: '{ProjectName}'", projectName);
    }

}

