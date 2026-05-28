using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WorldSimulator.RoutePathExtractor;

internal static class RouteDebugImageWriter
{
    public static void WriteDebugImage(string path, int width, int height, IReadOnlyList<GeneratedRoutePath> paths)
    {
        using var debug = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
        foreach (var route in paths)
        {
            var color = route.GenerationMethod switch
            {
                "forced_connector" => new Rgba32(255, 56, 56, 230),
                "connector" => new Rgba32(255, 178, 48, 210),
                _ when route.RouteType.Equals("sea", StringComparison.OrdinalIgnoreCase) => new Rgba32(38, 182, 218, 185),
                _ => new Rgba32(63, 220, 95, 185)
            };

            DrawPolyline(debug, route.PixelPoints, color, route.GenerationMethod == "forced_connector" ? 3 : 2);
        }

        debug.SaveAsPng(path);
    }

    public static void DrawPolyline(Image<Rgba32> image, IReadOnlyList<(int X, int Y)> points, Rgba32 color, int radius)
    {
        for (var i = 1; i < points.Count; i++)
        {
            foreach (var point in RouteGeometry.RasterLine(points[i - 1], points[i]))
            {
                DrawPoint(image, point.X, point.Y, color, radius);
            }
        }
    }

    public static void DrawPoint(Image<Rgba32> image, int x, int y, Rgba32 color, int radius)
    {
        for (var oy = -radius; oy <= radius; oy++)
        for (var ox = -radius; ox <= radius; ox++)
        {
            if ((ox * ox) + (oy * oy) > radius * radius) continue;
            var px = x + ox;
            var py = y + oy;
            if (px < 0 || py < 0 || px >= image.Width || py >= image.Height) continue;
            image[px, py] = color;
        }
    }
}
