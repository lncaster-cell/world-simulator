using System.Globalization;
using System.Windows.Data;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Converters;

public sealed class CaravanMarkerCanvasCoordinateConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 6
            || values[0] is not IReadOnlyList<MapPointViewModel> points
            || values[1] is not double progress
            || values[2] is not double containerWidth
            || values[3] is not double imageWidth
            || values[4] is not double containerHeight
            || values[5] is not double imageHeight)
        {
            return 0d;
        }

        var mapPoint = MainWindowViewModel.CalculatePointOnPolyline(points, progress);
        var mapLeft = (containerWidth - imageWidth) / 2d;
        var mapTop = (containerHeight - imageHeight) / 2d;
        var axis = parameter as string;
        return axis == "Y"
            ? mapTop + (mapPoint.Y * imageHeight) - 5d
            : mapLeft + (mapPoint.X * imageWidth) - 5d;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
