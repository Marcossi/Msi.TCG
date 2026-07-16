namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Representa una entrada (fichero o directorio) dentro de la carpeta de un proyecto.
/// </summary>
public sealed class FileEntry : IEquatable<FileEntry>
{
    /// <summary>
    /// Nombre del fichero o directorio.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ruta relativa desde la carpeta raíz del proyecto.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de entrada en el sistema de archivos.
    /// </summary>
    public FileType Type { get; set; }

    public bool Equals(FileEntry? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(RelativePath, other.RelativePath, StringComparison.OrdinalIgnoreCase)
            && Type == other.Type;
    }

    public override bool Equals(object? obj) => Equals(obj as FileEntry);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(RelativePath);
}
