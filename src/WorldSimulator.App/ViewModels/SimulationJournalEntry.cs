namespace WorldSimulator.App.ViewModels;

public sealed class SimulationJournalEntry
{
    public int Day { get; init; }
    public string CityId { get; init; } = "";
    public string CityName { get; init; } = "";
    public string CityState { get; init; } = "";
    public int PopulationStart { get; init; }
    public int PopulationEnd { get; init; }
    public int PopulationDelta { get; init; }
    public decimal FoodStart { get; init; }
    public decimal FoodEnd { get; init; }
    public decimal FoodDelta { get; init; }
    public int ActiveEventsCount { get; init; }
    public string Summary { get; init; } = "";
    public string FoodCalculation { get; init; } = "";
    public string EffectsTooltip { get; init; } = "";

    public string PopulationDisplay => $"{PopulationStart} → {PopulationEnd}";
    public string FoodDeltaDisplay => $"{FoodDelta:+0.##;-0.##;0}";
    public string FoodTooltip => $"Пища {FoodStart:0.##} → {FoodEnd:0.##}.{System.Environment.NewLine}{FoodCalculation}";
    public string EventsTooltip => string.IsNullOrWhiteSpace(EffectsTooltip) ? $"Активных событий: {ActiveEventsCount}" : EffectsTooltip;
    public string DayTooltip => $"{Summary}{System.Environment.NewLine}{string.Join(System.Environment.NewLine, Items.Select(i => $"• {i.Title}"))}";

    public IReadOnlyList<SimulationJournalItem> Items { get; init; } = [];
}

public sealed class SimulationJournalItem
{
    public SimulationJournalCategory Category { get; init; }
    public string Title { get; init; } = "";
    public string Details { get; init; } = "";
}

public enum SimulationJournalCategory
{
    Food,
    Event,
    Effects,
    Population,
    CityState,
    System,
    Error,
    Map,
    Debug
}

public enum SimulationJournalFilter
{
    All,
    Events,
    Population,
    Food,
    CityState,
    System,
    Errors,
    MapAndDebug
}
