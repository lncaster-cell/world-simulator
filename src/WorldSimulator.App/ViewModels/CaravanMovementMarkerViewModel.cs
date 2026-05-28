using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorldSimulator.App.ViewModels;

public sealed class CaravanMovementMarkerViewModel : INotifyPropertyChanged
{
    private double _progress;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RouteId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<MapPointViewModel> Points { get; set; } = [];

    public double Progress
    {
        get => _progress;
        set
        {
            var normalized = NormalizeProgress(value);
            if (Math.Abs(_progress - normalized) < 0.000001d)
            {
                return;
            }

            _progress = normalized;
            OnPropertyChanged();
        }
    }

    public decimal FoodMoved { get; set; }
    public decimal ResourcesMoved { get; set; }
    public decimal GoodsMoved { get; set; }
    public bool HasActiveFlow { get; set; }

    public bool IsSeaRoute => RouteId.EndsWith("_sea", StringComparison.OrdinalIgnoreCase);

    public decimal TotalVolume => FoodMoved + ResourcesMoved + GoodsMoved;

    private static double NormalizeProgress(double value)
    {
        var normalized = value % 1d;
        if (normalized < 0d)
        {
            normalized += 1d;
        }

        return normalized;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
