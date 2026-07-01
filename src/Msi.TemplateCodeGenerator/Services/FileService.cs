using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.Services;

// Servicio de infraestructura: encapsula la lectura y escritura de ficheros de texto en disco.
// No contiene lógica de dominio; es un adaptador fino sobre System.IO.File.

/// <summary>
/// Implementación de <see cref="IFileService"/> que accede directamente al sistema de ficheros.
/// </summary>
internal sealed class FileService(ILogger<FileService> logger) : IFileService
{
    private readonly ILogger<FileService> _logger = logger;
    /// <inheritdoc/>
    public async Task<string> ReadTextAsync(string filePath)
    {
        _logger.LogDebug("Leyendo fichero '{FilePath}'", filePath);
        string content = await File.ReadAllTextAsync(filePath);
        _logger.LogDebug("Fichero leído: '{FilePath}'", filePath);
        return content;
    }

    /// <inheritdoc/>
    public async Task WriteTextAsync(string filePath, string content)
    {
        _logger.LogDebug("Escribiendo fichero '{FilePath}'", filePath);
        await File.WriteAllTextAsync(filePath, content);
        _logger.LogDebug("Fichero escrito: '{FilePath}'", filePath);
    }
}
