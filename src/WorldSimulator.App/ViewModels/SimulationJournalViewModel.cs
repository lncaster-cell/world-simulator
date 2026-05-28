using System.Collections.ObjectModel;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;

namespace WorldSimulator.App.ViewModels;

public sealed class SimulationJournalViewModel : ViewModelBase
{
    private const int MaxSimulationJournalDays = 500;

    private SimulationJournalFilterOption _selectedSimulationJournalFilter = SimulationJournalFilterOption.All;
    private SimulationJournalEntry? _selectedSimulationJournalEntry;
    private string _selectedJournalCityId;
    private string _currentJournalCityName;

    public SimulationJournalViewModel(string selectedCityId, string currentJournalCityName)
    {
        _selectedJournalCityId = selectedCityId;
        _currentJournalCityName = currentJournalCityName;
    }

    public ObservableCollection<SimulationJournalEntry> SimulationJournalEntries { get; } = [];
    public ObservableCollection<SimulationJournalEntry> FilteredSimulationJournalEntries { get; } = [];
    public IReadOnlyList<SimulationJournalFilterOption> SimulationJournalFilters { get; } = SimulationJournalFilterOption.AllOptions;

    public SimulationJournalFilterOption SelectedSimulationJournalFilter
    {
        get => _selectedSimulationJournalFilter;
        set
        {
            if (_selectedSimulationJournalFilter == value) return;
            _selectedSimulationJournalFilter = value;
            OnPropertyChanged();
            RefreshSimulationJournalFilter();
        }
    }

    public SimulationJournalEntry? SelectedSimulationJournalEntry
    {
        get => _selectedSimulationJournalEntry;
        set
        {
            if (_selectedSimulationJournalEntry == value) return;
            _selectedSimulationJournalEntry = value;
            OnPropertyChanged();
        }
    }

    public string SelectedJournalCityId
    {
        get => _selectedJournalCityId;
        private set
        {
            if (_selectedJournalCityId == value) return;
            _selectedJournalCityId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentJournalCityName));
            OnPropertyChanged(nameof(CityJournalTitle));
            RefreshSimulationJournalFilter();
        }
    }

    public string CurrentJournalCityName => _currentJournalCityName;
    public string CityJournalTitle => $"Летопись города: {CurrentJournalCityName}";

    public void SelectCity(City city)
    {
        _currentJournalCityName = city.Name;
        SelectedJournalCityId = city.Id;
        OnPropertyChanged(nameof(CurrentJournalCityName));
        OnPropertyChanged(nameof(CityJournalTitle));
    }

    public void Reset(City city)
    {
        SimulationJournalEntries.Clear();
        FilteredSimulationJournalEntries.Clear();
        _currentJournalCityName = city.Name;
        SelectedJournalCityId = city.Id;
        SelectedSimulationJournalFilter = SimulationJournalFilterOption.All;
        SelectedSimulationJournalEntry = null;
        RefreshSimulationJournalFilter();
    }

    public void Clear()
    {
        SimulationJournalEntries.Clear();
        FilteredSimulationJournalEntries.Clear();
    }

    public void AppendSimulationJournalEntry(
        City city,
        int day,
        DailyFoodFlowResult foodResult,
        CityEventEffectsResult eventEffects,
        int populationStart,
        int populationEnd,
        CityState cityStateStart,
        CityState cityStateEnd,
        int activeEventsCount,
        IReadOnlyList<string> activeEventNamesBeforeAdvance,
        List<SimulationJournalItem> items,
        Func<DailyFoodFlowResult, string> buildFoodCalculationText,
        Func<DailyFoodFlowResult, IReadOnlyList<SimulationJournalItem>, int, int, string> buildJournalSummary,
        Func<CityState, string> toCityStateDisplay)
    {
        if (eventEffects.HasAnyEffect)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Effects,
                Title = "Применены эффекты событий",
                Details = $"Настроение {eventEffects.MoodDelta:+0;-0;0}, безопасность {eventEffects.SecurityDelta:+0;-0;0}, преступность {eventEffects.CrimeDelta:+0;-0;0}."
            });
        }

        if (populationStart != populationEnd)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Population,
                Title = "Изменение населения",
                Details = $"Население {populationStart} → {populationEnd} ({populationEnd - populationStart:+0;-0;0}), причина: {city.CityState switch { CityState.Famine => "голод", _ => "состояние города" }}."
            });
        }

        if (cityStateStart != cityStateEnd)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.CityState,
                Title = "Состояние города изменилось",
                Details = $"{toCityStateDisplay(cityStateStart)} → {toCityStateDisplay(cityStateEnd)}."
            });
        }

        var foodCalculationText = buildFoodCalculationText(foodResult);
        items.Add(new SimulationJournalItem
        {
            Category = SimulationJournalCategory.Food,
            Title = "Пищевой баланс дня",
            Details = foodCalculationText
        });

        var summary = cityStateEnd == CityState.Abandoned
            ? "Город опустел."
            : buildJournalSummary(foodResult, items, populationStart, populationEnd);

        var entry = new SimulationJournalEntry
        {
            Day = day,
            CityId = city.Id,
            CityName = city.Name,
            CityState = toCityStateDisplay(cityStateEnd),
            PopulationStart = populationStart,
            PopulationEnd = populationEnd,
            PopulationDelta = populationEnd - populationStart,
            FoodStart = foodResult.StartingFood,
            FoodEnd = foodResult.EndingFood,
            FoodDelta = foodResult.TotalDelta,
            ActiveEventsCount = activeEventsCount,
            Summary = summary,
            FoodCalculation = foodCalculationText,
            EffectsTooltip = activeEventNamesBeforeAdvance.Count == 0 ? "Событий нет." : $"События дня: {string.Join(", ", activeEventNamesBeforeAdvance)}.",
            Items = items
        };

        SimulationJournalEntries.Add(entry);
        if (SimulationJournalEntries.Count > MaxSimulationJournalDays)
        {
            SimulationJournalEntries.RemoveAt(0);
        }

        RefreshSimulationJournalFilter();
    }

    public void RefreshSimulationJournalFilter()
    {
        FilteredSimulationJournalEntries.Clear();
        foreach (var entry in SimulationJournalEntries.Where(entry => IsEntryInSelectedJournalCity(entry) && MatchesCurrentFilter(entry)))
        {
            FilteredSimulationJournalEntries.Add(entry);
        }
    }

    public bool MatchesCurrentFilter(SimulationJournalEntry entry)
    {
        return SelectedSimulationJournalFilter.Value switch
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

    private bool IsEntryInSelectedJournalCity(SimulationJournalEntry entry)
    {
        return string.Equals(entry.CityId, SelectedJournalCityId, StringComparison.OrdinalIgnoreCase);
    }
}
