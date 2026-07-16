namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contrato para la escritura de outputs generados por scripts Scriban.
/// </summary>
public interface IScriptOutputWriter
{
    /// <summary>
    /// Escribe contenido a un fichero.
    /// </summary>
    /// <param name="relativePath">Ruta relativa a la raíz del proyecto.</param>
    /// <param name="content">Contenido a escribir.</param>
    Task WriteToFile(string relativePath, string content);
}
