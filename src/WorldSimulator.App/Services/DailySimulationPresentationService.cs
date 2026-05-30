using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Simulation;

namespace WorldSimulator.App.Services;

public sealed class DailySimulationPresentationService
{
    private readonly CityEventEffectTextFormatter _eventEffectTextFormatter;

    public DailySimulationPresentationService(CityEventEffectTextFormatter eventEffectTextFormatter)
    {
        _eventEffectTextFormatter = eventEffectTextFormatter;
    }

    public DailySimulationPresentationResult Build(DailySimulationPresentationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SimulationResult.SelectedCityResult);

        var simulationResult = request.SimulationResult;
        var selectedCityResult = simulationResult.SelectedCityResult;
        var foodFlow = selectedCityResult.FoodFlow;
        var goodsCrafting = selectedCityResult.GoodsCrafting;
        var householdConsumption = selectedCityResult.HouseholdConsumption;
        var wealthFlow = selectedCityResult.WealthFlow;
        var technicalLogEntries = new List<string>();
        var journalItems = new List<SimulationJournalItem>();
        string? lastImportantChange = null;

        if (simulationResult.SelectedCityCrimeFlow?.Changed == true)
        {
            technicalLogEntries.Add($"День {request.Day}: преступность {simulationResult.SelectedCityCrimeFlow.StartingCrime} → {simulationResult.SelectedCityCrimeFlow.EndingCrime}; недельный баланс {simulationResult.SelectedCityCrimeFlow.ClampedDelta:+0;-0;0}.");
        }

        if (wealthFlow.TotalDelta != 0m)
        {
            technicalLogEntries.Add($"День {request.Day}: благосостояние {wealthFlow.StartingWealth:0.##} → {wealthFlow.EndingWealth:0.##}; баланс {wealthFlow.TotalDelta:+0.##;-0.##;0}." +
                                    $" Дефициты: еда {wealthFlow.FoodShortagePenalty:+0.##;-0.##;0}, товары {wealthFlow.GoodsShortagePenalty:+0.##;-0.##;0}, ресурсы {wealthFlow.ResourcesShortagePenalty:+0.##;-0.##;0}.");
        }

        if (goodsCrafting.GoodsProduced > 0m || goodsCrafting.ResourcesConsumed > 0m)
        {
            technicalLogEntries.Add($"День {request.Day}: товары +{goodsCrafting.GoodsProduced:0.##} произведены из ресурсов -{goodsCrafting.ResourcesConsumed:0.##}.");
        }
        else if (goodsCrafting.ResourcesAvailable <= 0m)
        {
            technicalLogEntries.Add($"День {request.Day}: производство товаров остановлено — нет ресурсов.");
        }

        if (householdConsumption.GoodsConsumed > 0m || householdConsumption.ResourcesConsumed > 0m)
        {
            technicalLogEntries.Add($"День {request.Day}: население потребило товары -{householdConsumption.GoodsConsumed:0.##} и ресурсы -{householdConsumption.ResourcesConsumed:0.##}.");
        }

        if (householdConsumption.HasAnyShortage)
        {
            technicalLogEntries.Add($"День {request.Day}: бытовой дефицит: товары не хватает {householdConsumption.GoodsShortage:0.##}, ресурсы не хватает {householdConsumption.ResourcesShortage:0.##}.");
        }

        foreach (var completedEvent in simulationResult.CompletedEvents)
        {
            technicalLogEntries.Add($"День {request.Day}: завершено событие “{completedEvent.Name}”.");
            lastImportantChange = $"День {request.Day}: событие “{completedEvent.Name}” завершилось.";
            journalItems.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Event,
                Title = $"Завершилось событие: {completedEvent.Name}",
                Details = $"Событие “{completedEvent.Name}” завершилось на дне {request.Day}."
            });
        }

        if (simulationResult.SelectedCityPopulationChange?.PopulationDelta is int populationDelta && populationDelta != 0)
        {
            var populationChange = simulationResult.SelectedCityPopulationChange;
            var populationMessage = $"День {request.Day}: население изменилось {populationChange.StartingPopulation} → {populationChange.EndingPopulation} ({populationChange.PopulationDelta:+0;-0;0}), причина: {populationChange.Reason}.";
            technicalLogEntries.Add(populationMessage);
            lastImportantChange = populationMessage;
        }

        if (simulationResult.GeneratedEvent is not null)
        {
            technicalLogEntries.Add($"День {request.Day}: случайное событие “{simulationResult.GeneratedEvent.Name}” началось в городе.");
            lastImportantChange = $"День {request.Day}: случайное событие “{simulationResult.GeneratedEvent.Name}” началось в городе.";
            journalItems.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Event,
                Title = $"Началось событие: {simulationResult.GeneratedEvent.Name}",
                Details = $"Случайное событие “{simulationResult.GeneratedEvent.Name}” началось в городе."
            });
        }

        if (simulationResult.SelectedCityEventEffects.HasAnyEffect)
        {
            technicalLogEntries.Add($"День {request.Day}: применены эффекты событий: {_eventEffectTextFormatter.Format(simulationResult.SelectedCityEventEffects)}.");
        }

        technicalLogEntries.Add($"День {request.Day}: пища {foodFlow.StartingFood:0.##} → {foodFlow.EndingFood:0.##}; баланс {foodFlow.TotalDelta:+0.##;-0.##;0} (потребление -{foodFlow.PopulationConsumption:0.##}, земледелие {foodFlow.AgricultureIncome:+0.##;-0.##;0}, рыбалка {foodFlow.FishingIncome:+0.##;-0.##;0}, охота {foodFlow.HuntingIncome:+0.##;-0.##;0}, поставки {foodFlow.MainlandSupplyIncome:+0.##;-0.##;0}, события {foodFlow.EventDelta:+0.##;-0.##;0}).");

        if (request.CityStateStart != request.CityStateEnd)
        {
            var stateChangeMessage = $"День {request.Day}: состояние города изменилось: {CityStateTextFormatter.ToRussian(request.CityStateStart)} → {CityStateTextFormatter.ToRussian(request.CityStateEnd)}.";
            technicalLogEntries.Add(stateChangeMessage);
            lastImportantChange = stateChangeMessage;
        }

        return new DailySimulationPresentationResult(
            technicalLogEntries,
            lastImportantChange,
            new SimulationJournalAppendRequest
            {
                Day = request.Day,
                City = request.City,
                FoodResult = foodFlow,
                EventEffects = simulationResult.SelectedCityEventEffects,
                PopulationStart = request.PopulationStart,
                PopulationEnd = request.PopulationEnd,
                CityStateStart = request.CityStateStart,
                CityStateEnd = request.CityStateEnd,
                ActiveEventsCount = request.ActiveEventsCount,
                ActiveEventNamesBeforeAdvance = simulationResult.ActiveEventNamesBeforeAdvance,
                Items = journalItems
            });
    }
}

public sealed class DailySimulationPresentationRequest
{
    public int Day { get; init; }
    public required City City { get; init; }
    public required WorldDayAdvanceResult SimulationResult { get; init; }
    public int PopulationStart { get; init; }
    public int PopulationEnd { get; init; }
    public CityState CityStateStart { get; init; }
    public CityState CityStateEnd { get; init; }
    public int ActiveEventsCount { get; init; }
}

public sealed record DailySimulationPresentationResult(
    IReadOnlyList<string> TechnicalLogEntries,
    string? LastImportantChange,
    SimulationJournalAppendRequest JournalAppendRequest);
