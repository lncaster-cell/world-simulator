using System.Globalization;
using System.Windows.Data;

namespace WorldSimulator.App.Converters;

public sealed class MapMarkerCanvasTopConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3
            || values[1] is not double containerHeight
            || values[2] is not double imageHeight
            || !TryReadCoordinate(values[0], out var y))
        {
            return 0d;
        }

        var mapTop = (containerHeight - imageHeight) / 2d;
        return mapTop + y * imageHeight - 11d;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();

    private static bool TryReadCoordinate(object value, out double coordinate)
    {
        switch (value)
        {
            case double doubleValue:
                coordinate = doubleValue;
                return true;
            case decimal decimalValue:
                coordinate = (double)decimalValue;
                return true;
            default:
                coordinate = 0d;
                return false;
        }
    }
}
