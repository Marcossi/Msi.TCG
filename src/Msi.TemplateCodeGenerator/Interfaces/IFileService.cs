namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contrato de infraestructura para operaciones de lectura y escritura de ficheros de texto.
/// Abstrae el acceso al sistema de ficheros para facilitar las pruebas y el desacoplamiento.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Lee el contenido completo de un fichero de texto.
    /// </summary>
    Task<string> ReadTextAsync(string filePath);

    /// <summary>
    /// Escribe contenido de texto en un fichero, sobrescribiendo su contenido si ya existe.
    /// </summary>
    Task WriteTextAsync(string filePath, string content);
}
