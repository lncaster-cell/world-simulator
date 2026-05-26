using System.Globalization;
using System.Windows.Data;

namespace WorldSimulator.App.Converters;

public sealed class MapMarkerCanvasLeftConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3
            || values[0] is not decimal x
            || values[1] is not double containerWidth
            || values[2] is not double imageWidth)
        {
            return 0d;
        }

        var mapLeft = (containerWidth - imageWidth) / 2d;
        return mapLeft + (double)x * imageWidth - 11d;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
