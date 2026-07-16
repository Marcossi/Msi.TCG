using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.Services.Templates;

/// <summary>
/// Gestiona la escritura de outputs desde scripts Scriban.
/// </summary>
internal sealed class ScriptOutputWriter(
    IFileSystem fileSystem,
    IProjectContext projectContext,
    ILogger<ScriptOutputWriter> logger) : IScriptOutputWriter
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IProjectContext _projectContext = projectContext;
    private readonly ILogger<ScriptOutputWriter> _logger = logger;

    /// <summary>
    /// Escribe contenido a un fichero.
    /// </summary>
    /// <param name="relativePath">Ruta relativa a la raíz del proyecto.</param>
    /// <param name="content">Contenido a escribir.</param>
    public async Task WriteToFile(string relativePath, string content)
    {
        string projectPath = _projectContext.CurrentProject?.FolderPath
            ?? throw new InvalidOperationException("No project is currently open");

        string fullPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));

        if (!fullPath.StartsWith(projectPath))
        {
            throw new InvalidOperationException(
                $"Output path '{relativePath}' is outside the project directory");
        }

        string? directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !await _fileSystem.DirectoryExistsAsync(directory))
        {
            await _fileSystem.CreateDirectoryAsync(directory);
        }

        await _fileSystem.WriteTextAsync(fullPath, content);

        _logger.LogInformation("Script wrote to {Path}", relativePath);
    }
}
