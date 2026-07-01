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
    IProjectSerializer serializer,
    IMessenger messenger,
    ILogger<ProjectService> logger) : IProjectService
{
    private readonly ILogger<ProjectService> _logger = logger;
    private readonly ProjectContext _context = (ProjectContext)context;
    private readonly IProjectSerializer _serializer = serializer;
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

        // TODO: Iniciar FileWatcher

        _context.CurrentProject = project;
        _context.CurrentProjectPath = projectPath;

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectOpenedMessage(projectPath));

        _logger.LogInformation("Proyecto abierto: '{ProjectPath}'", projectPath);
    }

    /// <summary>
    /// Cierra el proyecto activo.
    /// TODO: detener FileWatcher, limpiar recursos.
    /// </summary>
    public Task CloseProjectAsync()
    {
        _logger.LogInformation("Cerrando proyecto activo");

        // TODO: Detener FileWatcher, limpiar recursos (async)
        _context.CurrentProject = null;
        _context.CurrentProjectPath = null;

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
        _context.CurrentProjectPath = newProjectPath;

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
        _context.CurrentProject = project;
        _context.CurrentProjectPath = projectPath;

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectOpenedMessage(projectPath));

        _logger.LogInformation("Nuevo proyecto creado: '{ProjectName}'", projectName);
    }

}

