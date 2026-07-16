using System.Globalization;
using Avalonia.Data.Converters;

namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.Converters;

/// <summary>
/// Convierte un valor booleano a su inverso para visibilidad.
/// True → False, False → True.
/// </summary>
internal sealed class BoolToInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}
