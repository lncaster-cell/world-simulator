using System.Globalization;

namespace WorldSimulator.RoutePathExtractor;

internal static class RouteGeometry
{
    public static string FormatFixedOne(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    public static (int X, int Y) ToPixel((double X, double Y) pt, int width, int height)
        => ((int)Math.Round(pt.X * (width - 1)), (int)Math.Round(pt.Y * (height - 1)));

    public static double Distance((int X, int Y) a, (int X, int Y) b)
        => Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));

    public static List<(int X, int Y)> RasterLine((int X, int Y) a, (int X, int Y) b)
    {
        var points = new List<(int X, int Y)>();
        var x0 = a.X;
        var y0 = a.Y;
        var x1 = b.X;
        var y1 = b.Y;
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;
        while (true)
        {
            points.Add((x0, y0));
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        return points;
    }

    public static Bounds BuildBounds((int X, int Y) s, (int X, int Y) t, int margin, int width, int height)
        => new(
            Math.Max(0, Math.Min(s.X, t.X) - margin),
            Math.Min(width - 1, Math.Max(s.X, t.X) + margin),
            Math.Max(0, Math.Min(s.Y, t.Y) - margin),
            Math.Min(height - 1, Math.Max(s.Y, t.Y) + margin));
}
