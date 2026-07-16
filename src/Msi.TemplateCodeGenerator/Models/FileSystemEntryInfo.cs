namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa una entrada del sistema de ficheros (fichero o directorio)
/// con su ruta completa, nombre y tipo.
/// </summary>
public sealed record FileSystemEntryInfo(string FullPath, string Name, bool IsDirectory);
