using CommunityToolkit.Mvvm.Messaging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Servicio que gestiona las operaciones relacionadas con proyectos.
/// Actualiza el IProjectContext y maneja FileWatcher, carga/guardado, validaciones, etc.
/// Notifica cambios mediante mensajería (IMessenger).
/// </summary>
internal sealed class ProjectService : IProjectService
{
    private readonly ProjectContext _context;
    private readonly IProjectSerializer _serializer;
    private readonly IMessenger _messenger;

    public ProjectService(IProjectContext context, IProjectSerializer serializer, IMessenger messenger)
    {
        // Downcasting seguro porque registramos ProjectContext como singleton
        _context = (ProjectContext)context;
        _serializer = serializer;
        _messenger = messenger;
    }

    /// <summary>
    /// Abre un proyecto desde la ruta especificada.
    /// </summary>
    public async Task OpenProjectAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
        }

        // Cargar proyecto desde disco usando el serializador
        var project = await _serializer.LoadAsync(projectPath);

        // TODO: Iniciar FileWatcher

        _context.CurrentProject = project;
        _context.CurrentProjectPath = projectPath;

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectOpenedMessage(projectPath));
    }

    /// <summary>
    /// Cierra el proyecto activo.
    /// TODO: detener FileWatcher, limpiar recursos.
    /// </summary>
    public Task CloseProjectAsync()
    {
        // TODO: Detener FileWatcher, limpiar recursos (async)
        _context.CurrentProject = null;
        _context.CurrentProjectPath = null;

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectClosedMessage());

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

        await _serializer.SaveAsync(_context.CurrentProject!, _context.CurrentProjectPath);

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectSavedMessage(_context.CurrentProjectPath));
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

        await _serializer.SaveAsync(_context.CurrentProject!, newProjectPath);

        // Actualizar la ruta actual después de guardar
        _context.CurrentProjectPath = newProjectPath;

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectSavedMessage(newProjectPath));
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

        // Crear nuevo proyecto
        var project = new Models.Project
        {
            Name = projectName
        };

        // Guardar el proyecto en disco
        await _serializer.SaveAsync(project, projectPath);

        // TODO: Crear estructura de carpetas adicionales si es necesario

        // Actualizar el contexto
        _context.CurrentProject = project;
        _context.CurrentProjectPath = projectPath;

        // Notificar a toda la aplicación
        _messenger.Send(new ProjectOpenedMessage(projectPath));
    }
}
