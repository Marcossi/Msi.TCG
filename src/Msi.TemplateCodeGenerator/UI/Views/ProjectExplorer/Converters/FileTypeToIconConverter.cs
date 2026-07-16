using System.Globalization;
using Avalonia.Data.Converters;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.Converters;

/// <summary>
/// Conversor que transforma un <see cref="FileType"/> en el glifo de icono correspondiente.
/// Usa puntos de código de Segoe MDL2 Assets / Segoe Fluent Icons (Private Use Area).
/// El TextBlock que consuma este conversor debe tener FontFamily apuntando a esa fuente.
/// </summary>
internal sealed class FileTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FileType type ? type switch
        {
            FileType.Project   => "\uEB3B",   // GenericApp
            FileType.Directory => "\uE8B7",   // Folder
            FileType.Script    => "\uE89A",   // TwoPage
            FileType.Data      => "\uE8D6",   // Database
            FileType.Metadata  => "\uE8D6",   // Database
            FileType.Other     => "\uE8A5",   // Document
            _                  => string.Empty
        } : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
