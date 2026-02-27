using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.UI.Converters;

/// <summary>
/// Conversor que transforma un <see cref="FileType"/> en el pincel de primer plano correspondiente.
/// Proyecto en azul; carpetas en amarillo; scripts en verde; el resto hereda el color del tema.
/// Nota: los emojis de color (📂, 📁, 📄) ignoran Foreground en la mayoría de plataformas;
/// los símbolos Unicode como Ⓢ sí lo respetan.
/// </summary>
internal sealed class FileTypeToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FileType type ? type switch
        {
            FileType.Project   => Brushes.CornflowerBlue,
            FileType.Directory => Brushes.DarkGoldenrod,
            FileType.Script    => Brushes.Green,
            FileType.Other     => Brushes.Black,
            _                  => null
        } : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
