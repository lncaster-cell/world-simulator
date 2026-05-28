using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;

namespace WorldSimulator.App.Services;

public sealed class SimulationJournalService
{
    public const int MaxSimulationJournalDays = 500;

    public SimulationJournalEntry BuildEntry(SimulationJournalAppendRequest request)
    {
        var items = CategorizeItems(request);
        var foodCalculation = BuildFoodCalculationText(request.FoodResult);
        var summary = request.CityStateEnd == CityState.Abandoned
            ? "Город опустел."
            : BuildJournalSummary(request.FoodResult, items, request.PopulationStart, request.PopulationEnd);

        return new SimulationJournalEntry
        {
            Day = request.Day,
            CityId = request.City.Id,
            CityName = request.City.Name,
            CityState = ToRussianCityState(request.CityStateEnd),
            PopulationStart = request.PopulationStart,
            PopulationEnd = request.PopulationEnd,
            PopulationDelta = request.PopulationEnd - request.PopulationStart,
            FoodStart = request.FoodResult.StartingFood,
            FoodEnd = request.FoodResult.EndingFood,
            FoodDelta = request.FoodResult.TotalDelta,
            ActiveEventsCount = request.ActiveEventsCount,
            Summary = summary,
            FoodCalculation = foodCalculation,
            EffectsTooltip = request.ActiveEventNamesBeforeAdvance.Count == 0
                ? "Событий нет."
                : $"События дня: {string.Join(", ", request.ActiveEventNamesBeforeAdvance)}.",
            Items = items
        };
    }

    public void TrimToMaxDays(IList<SimulationJournalEntry> entries)
    {
        while (entries.Count > MaxSimulationJournalDays)
        {
            entries.RemoveAt(0);
        }
    }

    public static bool IsEntryInCategory(SimulationJournalEntry entry, SimulationJournalFilter filter)
    {
        return filter switch
        {
            SimulationJournalFilter.All => true,
            SimulationJournalFilter.Events => entry.Items.Any(i => i.Category is SimulationJournalCategory.Event or SimulationJournalCategory.Effects),
            SimulationJournalFilter.Population => entry.Items.Any(i => i.Category == SimulationJournalCategory.Population),
            SimulationJournalFilter.Food => entry.Items.Any(i => i.Category == SimulationJournalCategory.Food),
            SimulationJournalFilter.CityState => entry.Items.Any(i => i.Category == SimulationJournalCategory.CityState),
            SimulationJournalFilter.System => entry.Items.Any(i => i.Category == SimulationJournalCategory.System),
            SimulationJournalFilter.Errors => entry.Items.Any(i => i.Category == SimulationJournalCategory.Error),
            SimulationJournalFilter.MapAndDebug => entry.Items.Any(i => i.Category is SimulationJournalCategory.Map or SimulationJournalCategory.Debug),
            _ => true
        };
    }

    private static IReadOnlyList<SimulationJournalItem> CategorizeItems(SimulationJournalAppendRequest request)
    {
        var items = new List<SimulationJournalItem>(request.Items);

        if (request.EventEffects.HasAnyEffect)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Effects,
                Title = "Применены эффекты событий",
                Details = $"Настроение {request.EventEffects.MoodDelta:+0;-0;0}, безопасность {request.EventEffects.SecurityDelta:+0;-0;0}, преступность {request.EventEffects.CrimeDelta:+0;-0;0}."
            });
        }

        if (request.PopulationStart != request.PopulationEnd)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Population,
                Title = "Изменение населения",
                Details = $"Население {request.PopulationStart} → {request.PopulationEnd} ({request.PopulationEnd - request.PopulationStart:+0;-0;0}), причина: {request.CityStateEnd switch { CityState.Famine => "голод", _ => "состояние города" }}."
            });
        }

        if (request.CityStateStart != request.CityStateEnd)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.CityState,
                Title = "Состояние города изменилось",
                Details = $"{ToRussianCityState(request.CityStateStart)} → {ToRussianCityState(request.CityStateEnd)}."
            });
        }

        items.Add(new SimulationJournalItem
        {
            Category = SimulationJournalCategory.Food,
            Title = "Пищевой баланс дня",
            Details = BuildFoodCalculationText(request.FoodResult)
        });

        return items;
    }

    private static string BuildFoodCalculationText(DailyFoodFlowResult result) =>
        $"Потребление: -{result.PopulationConsumption:0.##}.{Environment.NewLine}Земледелие: {result.AgricultureIncome:+0.##;-0.##;0}.{Environment.NewLine}Рыбалка: {result.FishingIncome:+0.##;-0.##;0}.{Environment.NewLine}Охота: {result.HuntingIncome:+0.##;-0.##;0}.{Environment.NewLine}Поставки: {result.MainlandSupplyIncome:+0.##;-0.##;0}.{Environment.NewLine}События: {result.EventDelta:+0.##;-0.##;0}.";

    private static string BuildJournalSummary(DailyFoodFlowResult foodResult, IReadOnlyList<SimulationJournalItem> items, int populationStart, int populationEnd)
    {
        var eventItem = items.FirstOrDefault(i => i.Category == SimulationJournalCategory.Event);
        if (populationStart != populationEnd)
        {
            return $"Население {populationStart} → {populationEnd}. Пища {foodResult.TotalDelta:+0.##;-0.##;0}.";
        }

        if (eventItem is not null)
        {
            return $"{eventItem.Title}. Пища {foodResult.TotalDelta:+0.##;-0.##;0}.";
        }

        return $"Пища {foodResult.TotalDelta:+0.##;-0.##;0}, событий нет.";
    }

    private static string ToRussianCityState(CityState cityState)
    {
        return cityState switch
        {
            CityState.Stable => "Стабильность",
            CityState.Prosperous => "Процветание",
            CityState.Stagnation => "Стагнация",
            CityState.FoodShortage => "Нехватка пищи",
            CityState.Famine => "Голод",
            CityState.EconomicDecline => "Экономический спад",
            CityState.CrimeProblem => "Проблемы с преступностью",
            CityState.Unrest => "Беспорядки",
            CityState.Recovery => "Восстановление",
            CityState.Collapse => "Коллапс",
            CityState.Abandoned => "Опустевший город",
            _ => cityState.ToString()
        };
    }
}

public sealed class SimulationJournalAppendRequest
{
    public int Day { get; init; }
    public required City City { get; init; }
    public required DailyFoodFlowResult FoodResult { get; init; }
    public required CityEventEffectsResult EventEffects { get; init; }
    public int PopulationStart { get; init; }
    public int PopulationEnd { get; init; }
    public CityState CityStateStart { get; init; }
    public CityState CityStateEnd { get; init; }
    public int ActiveEventsCount { get; init; }
    public IReadOnlyList<string> ActiveEventNamesBeforeAdvance { get; init; } = [];
    public IReadOnlyList<SimulationJournalItem> Items { get; init; } = [];
}
