using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WorldSimulator.App.Services;
using WorldSimulator.Core.Cities;

namespace WorldSimulator.App.ViewModels;

public sealed class SimulationJournalViewModel : INotifyPropertyChanged
{
    private readonly SimulationJournalService _journalService;
    private SimulationJournalFilterOption _selectedSimulationJournalFilter = SimulationJournalFilterOption.All;
    private SimulationJournalEntry? _selectedSimulationJournalEntry;
    private string _selectedJournalCityId;
    private string _currentJournalCityName;

    public SimulationJournalViewModel(SimulationJournalService journalService, string selectedJournalCityId, string currentJournalCityName)
    {
        _journalService = journalService;
        _selectedJournalCityId = selectedJournalCityId;
        _currentJournalCityName = currentJournalCityName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SimulationJournalEntry> SimulationJournalEntries { get; } = new();

    public ObservableCollection<SimulationJournalEntry> FilteredSimulationJournalEntries { get; } = new();

    public IReadOnlyList<SimulationJournalFilterOption> SimulationJournalFilterOptions { get; } = SimulationJournalFilterOption.AllOptions;

    public SimulationJournalFilterOption SelectedSimulationJournalFilter
    {
        get => _selectedSimulationJournalFilter;
        set
        {
            if (_selectedSimulationJournalFilter == value)
            {
                return;
            }

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
            if (_selectedSimulationJournalEntry == value)
            {
                return;
            }

            _selectedSimulationJournalEntry = value;
            OnPropertyChanged();
        }
    }

    public string SelectedJournalCityId
    {
        get => _selectedJournalCityId;
        private set
        {
            if (_selectedJournalCityId == value)
            {
                return;
            }

            _selectedJournalCityId = value;
            OnPropertyChanged();
            RefreshSimulationJournalFilter();
        }
    }

    public string CurrentJournalCityName
    {
        get => _currentJournalCityName;
        private set
        {
            if (_currentJournalCityName == value)
            {
                return;
            }

            _currentJournalCityName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CityJournalTitle));
        }
    }

    public string CityJournalTitle => $"Летопись города: {CurrentJournalCityName}";

    public void SelectCity(City city)
    {
        ArgumentNullException.ThrowIfNull(city);
        SelectCity(city.Id, city.Name);
    }

    public void SelectCity(string cityId, string cityName)
    {
        SelectedJournalCityId = cityId;
        CurrentJournalCityName = cityName;
    }

    public void Append(SimulationJournalAppendRequest request)
    {
        SimulationJournalEntries.Add(_journalService.BuildEntry(request));
        _journalService.TrimToMaxDays(SimulationJournalEntries);
        RefreshSimulationJournalFilter();
    }

    public void Clear()
    {
        SimulationJournalEntries.Clear();
        FilteredSimulationJournalEntries.Clear();
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
        return SimulationJournalService.IsEntryInCategory(entry, SelectedSimulationJournalFilter.Value);
    }

    public bool IsEntryInSelectedJournalCity(SimulationJournalEntry entry)
    {
        return string.Equals(entry.CityId, SelectedJournalCityId, StringComparison.OrdinalIgnoreCase);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SimulationJournalFilterOption
{
    public static readonly SimulationJournalFilterOption All = new(SimulationJournalFilter.All, "Все");
    public static readonly SimulationJournalFilterOption Events = new(SimulationJournalFilter.Events, "События");
    public static readonly SimulationJournalFilterOption Population = new(SimulationJournalFilter.Population, "Население");
    public static readonly SimulationJournalFilterOption Food = new(SimulationJournalFilter.Food, "Пища");
    public static readonly SimulationJournalFilterOption CityState = new(SimulationJournalFilter.CityState, "Состояние");
    public static readonly SimulationJournalFilterOption System = new(SimulationJournalFilter.System, "Система");
    public static readonly SimulationJournalFilterOption Errors = new(SimulationJournalFilter.Errors, "Ошибки");
    public static readonly SimulationJournalFilterOption MapAndDebug = new(SimulationJournalFilter.MapAndDebug, "Карта/отладка");
    public static readonly IReadOnlyList<SimulationJournalFilterOption> AllOptions = [All, Events, Population, Food, CityState, System, Errors, MapAndDebug];

    public SimulationJournalFilterOption(SimulationJournalFilter value, string title)
    {
        Value = value;
        Title = title;
    }

    public SimulationJournalFilter Value { get; }

    public string Title { get; }
}