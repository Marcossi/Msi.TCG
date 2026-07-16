using System.Text.Json;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services;

/// <summary>
/// Persiste y restaura el estado de UI del explorador de proyectos
/// en un fichero dentro de la carpeta .tcg/ del proyecto.
/// </summary>
internal sealed class ProjectExplorerStateService(ILogger<ProjectExplorerStateService> logger, IFileSystem fileSystem) : IProjectExplorerStateService
{
    private readonly ILogger<ProjectExplorerStateService> _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public async Task SaveStateAsync(string projectPath, ProjectExplorerState state)
    {
        string stateFilePath = GetStateFilePath(projectPath);

        _logger.LogDebug("Guardando estado del explorador en '{FilePath}'", stateFilePath);

        string json = JsonSerializer.Serialize(state, JsonOptions);
        await _fileSystem.WriteTextAsync(stateFilePath, json);

        _logger.LogDebug("Estado del explorador guardado");
    }

    /// <inheritdoc/>
    public async Task<ProjectExplorerState?> LoadStateAsync(string projectPath)
    {
        string stateFilePath = GetStateFilePath(projectPath);

        if (!await _fileSystem.FileExistsAsync(stateFilePath))
        {
            _logger.LogDebug("No existe fichero de estado para '{ProjectPath}'", projectPath);
            return null;
        }

        _logger.LogDebug("Cargando estado del explorador desde '{FilePath}'", stateFilePath);

        string json = await _fileSystem.ReadTextAsync(stateFilePath);
        ProjectExplorerState? state = JsonSerializer.Deserialize<ProjectExplorerState>(json, JsonOptions);

        _logger.LogDebug("Estado del explorador cargado ({Count} carpetas expandidas)",
            state?.ExpandedPaths.Count ?? 0);

        return state;
    }

    /// <inheritdoc/>
    public async Task EnsureEditorDirectoriesExistAsync(string projectPath)
    {
        string projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));

        string stateDirectory = Path.Combine(projectDirectory, ProjectDirectoryConstants.StateFolderPath);

        if (!await _fileSystem.DirectoryExistsAsync(stateDirectory))
        {
            await _fileSystem.CreateDirectoryAsync(stateDirectory);
            _logger.LogInformation("Created editor state directory: {Path}", stateDirectory);
        }
    }

    private static string GetStateFilePath(string projectPath)
    {
        string directory = Path.GetDirectoryName(projectPath)
            ?? throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
        return Path.Combine(directory, ProjectDirectoryConstants.ExplorerStateRelativePath);
    }
}
