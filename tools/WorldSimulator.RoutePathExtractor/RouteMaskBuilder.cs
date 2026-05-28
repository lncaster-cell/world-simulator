using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WorldSimulator.RoutePathExtractor;

internal sealed class RouteMaskBuilder
{
    private readonly RouteExtractionOptions _options;

    public RouteMaskBuilder(RouteExtractionOptions options) => _options = options;

    public bool[] BuildMask(Image<Rgba32> image, Func<Rgba32, bool> pred)
    {
        var mask = new bool[image.Width * image.Height];
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            mask[(y * image.Width) + x] = pred(image[x, y]);
        return mask;
    }

    public bool[] DilateMask(bool[] mask, int w, int h, int radius)
    {
        var result = new bool[mask.Length];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            if (!mask[(y * w) + x]) continue;
            for (var oy = -radius; oy <= radius; oy++)
            {
                var ny = y + oy;
                if (ny < 0 || ny >= h) continue;
                for (var ox = -radius; ox <= radius; ox++)
                {
                    var nx = x + ox;
                    if (nx < 0 || nx >= w) continue;
                    if ((ox * ox) + (oy * oy) > (radius * radius)) continue;
                    result[(ny * w) + nx] = true;
                }
            }
        }
        return result;
    }

    public AnchorSearchResult? FindNearestMaskPixel(bool[] mask, int width, int height, (double X, double Y) pt)
    {
        var center = RouteGeometry.ToPixel(pt, width, height);
        var bestDistSq = double.MaxValue;
        (int, int)? best = null;
        foreach (var radius in new[] { _options.AnchorSearchRadiusSoftPx, 80, _options.AnchorSearchRadiusHardPx })
        {
            var foundAny = false;
            for (var y = Math.Max(0, center.Y - radius); y <= Math.Min(height - 1, center.Y + radius); y++)
            for (var x = Math.Max(0, center.X - radius); x <= Math.Min(width - 1, center.X + radius); x++)
            {
                if (!mask[(y * width) + x]) continue;
                foundAny = true;
                var d = ((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y));
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = (x, y);
                }
            }

            if (foundAny && best.HasValue)
                return new AnchorSearchResult(best.Value, Math.Sqrt(bestDistSq));
        }

        return null;
    }

    public static bool IsRoad(Rgba32 p) => p.A >= 128 && p.R >= 180 && p.B >= 180 && p.G <= 120;

    public static bool IsSea(Rgba32 p) => p.A >= 128 && p.G >= 180 && p.B >= 180 && p.R <= 120;
}
