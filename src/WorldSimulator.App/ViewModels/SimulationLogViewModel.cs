using System.Collections.ObjectModel;

namespace WorldSimulator.App.ViewModels;

public sealed class SimulationLogViewModel : ViewModelBase
{
    public ObservableCollection<string> TechnicalLogEntries { get; } = new();
    public ObservableCollection<string> ActiveEventEntries { get; } = new();
    public ObservableCollection<string> CompletedEventEntries { get; } = new();

    public bool HasTechnicalLogEntries => TechnicalLogEntries.Count > 0;
    public bool HasActiveEventEntries => ActiveEventEntries.Count > 0;
    public bool HasCompletedEventEntries => CompletedEventEntries.Count > 0;

    public void AddTechnicalEntry(string message, int maxEntries)
    {
        TechnicalLogEntries.Add(message);

        if (TechnicalLogEntries.Count > maxEntries)
        {
            TechnicalLogEntries.RemoveAt(0);
        }

        OnPropertyChanged(nameof(HasTechnicalLogEntries));
    }

    public void ClearSimulationEntries()
    {
        TechnicalLogEntries.Clear();
        ActiveEventEntries.Clear();
        CompletedEventEntries.Clear();
        RefreshAvailability();
    }

    public void RefreshActiveAndCompletedAvailability()
    {
        OnPropertyChanged(nameof(HasActiveEventEntries));
        OnPropertyChanged(nameof(HasCompletedEventEntries));
    }

    private void RefreshAvailability()
    {
        OnPropertyChanged(nameof(HasTechnicalLogEntries));
        RefreshActiveAndCompletedAvailability();
    }
}
