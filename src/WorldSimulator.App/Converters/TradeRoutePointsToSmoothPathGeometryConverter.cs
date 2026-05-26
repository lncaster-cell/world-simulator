using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Converters;

public sealed class TradeRoutePointsToSmoothPathGeometryConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3
            || values[0] is not IReadOnlyList<MapPointViewModel> points
            || points.Count < 2
            || values[1] is not double containerWidth
            || values[2] is not double imageWidth)
        {
            return Geometry.Empty;
        }

        var containerHeight = values.Length > 3 && values[3] is double ch ? ch : 0d;
        var imageHeight = values.Length > 4 && values[4] is double ih ? ih : 0d;
        var mapLeft = (containerWidth - imageWidth) / 2d;
        var mapTop = (containerHeight - imageHeight) / 2d;

        var absolute = points.Select(p => new Point(mapLeft + (p.X * imageWidth), mapTop + (p.Y * imageHeight))).ToList();
        var figure = new PathFigure { StartPoint = absolute[0], IsClosed = false, IsFilled = false };
        if (absolute.Count == 2)
        {
            figure.Segments.Add(new LineSegment(absolute[1], true));
        }
        else
        {
            for (var i = 0; i < absolute.Count - 1; i++)
            {
                var p0 = i > 0 ? absolute[i - 1] : absolute[i];
                var p1 = absolute[i];
                var p2 = absolute[i + 1];
                var p3 = i + 2 < absolute.Count ? absolute[i + 2] : p2;

                var c1 = new Point(p1.X + ((p2.X - p0.X) / 6d), p1.Y + ((p2.Y - p0.Y) / 6d));
                var c2 = new Point(p2.X - ((p3.X - p1.X) / 6d), p2.Y - ((p3.Y - p1.Y) / 6d));
                figure.Segments.Add(new BezierSegment(c1, c2, p2, true));
            }
        }

        return new PathGeometry([figure]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
