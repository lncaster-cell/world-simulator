using System.Windows.Controls;
using System.Windows.Input;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Views;

/// <summary>
/// Expects <see cref="MainWindowViewModel" /> as its inherited DataContext.
/// </summary>
public partial class WorldMapView : UserControl
{
    public WorldMapView()
    {
        InitializeComponent();
    }

    private void MapContainer_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
    }

    private void MapImage_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
    }

    private void MapContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var containerWidth = MapContainer.ActualWidth;
        var containerHeight = MapContainer.ActualHeight;
        var imageWidth = MapImage.ActualWidth;
        var imageHeight = MapImage.ActualHeight;

        if (containerWidth <= 0 || containerHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return;
        }

        var mapLeft = (containerWidth - imageWidth) / 2d;
        var mapTop = (containerHeight - imageHeight) / 2d;

        var click = e.GetPosition(MapContainer);

        if (click.X < mapLeft || click.Y < mapTop || click.X > mapLeft + imageWidth || click.Y > mapTop + imageHeight)
        {
            return;
        }

        var relativeX = (click.X - mapLeft) / imageWidth;
        var relativeY = (click.Y - mapTop) / imageHeight;

        relativeX = Math.Clamp(relativeX, 0d, 1d);
        relativeY = Math.Clamp(relativeY, 0d, 1d);

        if (viewModel.IsMapCalibrationModeEnabled)
        {
            viewModel.RegisterMapCalibrationPoint(relativeX, relativeY);
        }

        if (viewModel.IsTradeRouteAuthoringModeEnabled)
        {
            viewModel.RegisterTradeRouteAuthoringPoint(relativeX, relativeY);
        }
    }
}
