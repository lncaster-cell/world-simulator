using System.Globalization;
using System.Windows.Data;

namespace WorldSimulator.App.Converters;

public sealed class MapMarkerCanvasTopConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3
            || values[0] is not decimal y
            || values[1] is not double containerHeight
            || values[2] is not double imageHeight)
        {
            return 0d;
        }

        var mapTop = (containerHeight - imageHeight) / 2d;
        return mapTop + (double)y * imageHeight - 11d;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
