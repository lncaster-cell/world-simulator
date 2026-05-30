using WorldSimulator.Core.Events;

namespace WorldSimulator.App.Services;

public sealed class CityEventEffectTextFormatter
{
    public string Format(CityEventEffectsResult effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        var segments = new List<string>();

        if (effects.FoodDelta != 0m)
        {
            segments.Add($"пища {effects.FoodDelta:+0.##;-0.##;0}");
        }

        if (effects.MoodDelta != 0)
        {
            segments.Add($"настроение {effects.MoodDelta:+0;-0;0}");
        }

        if (effects.SecurityDelta != 0)
        {
            segments.Add($"безопасность {effects.SecurityDelta:+0;-0;0}");
        }

        if (effects.CrimeDelta != 0)
        {
            segments.Add($"преступность {effects.CrimeDelta:+0;-0;0}");
        }

        if (effects.WealthDelta != 0m)
        {
            segments.Add($"богатство {effects.WealthDelta:+0.##;-0.##;0}");
        }

        if (effects.ResourcesDelta != 0m)
        {
            segments.Add($"ресурсы {effects.ResourcesDelta:+0.##;-0.##;0}");
        }

        if (effects.MainlandSupplyDelta != 0m)
        {
            segments.Add($"поставки с материка {effects.MainlandSupplyDelta:+0.##;-0.##;0}");
        }

        return segments.Count == 0 ? "без эффектов" : string.Join(", ", segments);
    }
}
