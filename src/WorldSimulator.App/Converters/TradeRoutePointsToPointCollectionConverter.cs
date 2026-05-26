using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Converters;

public sealed class TradeRoutePointsToPointCollectionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3
            || values[0] is not IReadOnlyList<MapPointViewModel> points
            || values[1] is not double containerWidth
            || values[2] is not double imageWidth)
        {
            return new PointCollection();
        }

        var containerHeight = values.Length > 3 && values[3] is double ch ? ch : 0d;
        var imageHeight = values.Length > 4 && values[4] is double ih ? ih : 0d;
        var mapLeft = (containerWidth - imageWidth) / 2d;
        var mapTop = (containerHeight - imageHeight) / 2d;

        return new PointCollection(points.Select(p => new Point(mapLeft + (p.X * imageWidth), mapTop + (p.Y * imageHeight))));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
