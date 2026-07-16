using System.Text.Json;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Constants;
using Msi.TemplateCodeGenerator.Interfaces;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Services.Project;

/// <summary>
/// Serializador de proyectos en formato JSON (JSONC con soporte para comentarios en lectura).
/// NOTA: Los comentarios se leen pero NO se preservan al guardar.
/// TODO: Evaluar migración a JSON5 si se requiere preservar comentarios.
/// </summary>
internal sealed class JsonProjectSerializer(ILogger<JsonProjectSerializer> logger, IFileSystem fileSystem) : IProjectSerializer
{
    private readonly ILogger<JsonProjectSerializer> _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip, // Permite leer comentarios (JSONC)
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// DTO interno que envuelve el proyecto con metadata de persistencia.
    /// </summary>
    private sealed class ProjectFileDto
    {
        public int FileFormatVersion { get; set; } = ProjectConstants.CurrentFileFormatVersion;
        public ProjectModel Project { get; set; } = null!;
    }

    /// <summary>
    /// Guarda un proyecto en formato JSON.
    /// </summary>
    public async Task SaveAsync(ProjectModel project, string filePath)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        _logger.LogInformation("Guardando proyecto en '{FilePath}'", filePath);

        // Crear el DTO con versión
        ProjectFileDto dto = new()
        {
            FileFormatVersion = ProjectConstants.CurrentFileFormatVersion,
            Project = project
        };

        // Serializar a JSON
        string json = JsonSerializer.Serialize(dto, _options);

        // Escribir a disco
        await _fileSystem.WriteTextAsync(filePath, json);

        _logger.LogInformation("Proyecto guardado en '{FilePath}'", filePath);
    }

    /// <summary>
    /// Carga un proyecto desde un archivo JSON.
    /// </summary>
    public async Task<ProjectModel> LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        if (!await _fileSystem.FileExistsAsync(filePath))
            throw new FileNotFoundException("Project file not found.", filePath);

        _logger.LogInformation("Cargando proyecto desde '{FilePath}'", filePath);

        // Leer archivo JSON
        string json = await _fileSystem.ReadTextAsync(filePath);

        // Deserializar DTO
        ProjectFileDto? dto = JsonSerializer.Deserialize<ProjectFileDto>(json, _options);
        if (dto == null)
            throw new InvalidOperationException("Failed to deserialize project file.");

        // Validar versión del formato
        if (dto.FileFormatVersion > ProjectConstants.CurrentFileFormatVersion)
        {
            throw new NotSupportedException(
                $"Project file format version {dto.FileFormatVersion} is not supported. " +
                $"Current version: {ProjectConstants.CurrentFileFormatVersion}. " +
                "Please update the application to open this file.");
        }

        if (dto.FileFormatVersion < ProjectConstants.MinimumSupportedFileFormatVersion)
        {
            throw new NotSupportedException(
                $"Project file format version {dto.FileFormatVersion} is too old and requires migration. " +
                $"Minimum supported version: {ProjectConstants.MinimumSupportedFileFormatVersion}.");
        }

        // TODO: Si en el futuro hay versiones intermedias, aplicar migraciones aquí
        // if (dto.FileFormatVersion == 1)
        //     dto.Project = MigrateFromVersion1ToVersion2(dto.Project);

        if (dto.Project == null)
            throw new InvalidOperationException("Project data is missing in the file.");

        _logger.LogInformation("Proyecto cargado desde '{FilePath}'", filePath);

        return dto.Project;
    }
}
