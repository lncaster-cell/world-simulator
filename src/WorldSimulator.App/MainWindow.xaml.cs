using System.Windows.Input;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App;

public partial class MainWindow : System.Windows.Window
{
    private const double GothaMapX = 0.72;
    private const double GothaMapY = 0.23;

    private CityWindow? _cityWindow;
    private LogWindow? _logWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Loaded += (_, _) => UpdateGothaMarkerPosition();
    }

    private void MapContainer_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        UpdateGothaMarkerPosition();
    }

    private void MapImage_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        UpdateGothaMarkerPosition();
    }

    private void UpdateGothaMarkerPosition()
    {
        if (MapContainer.ActualWidth <= 0 || MapContainer.ActualHeight <= 0 || MapImage.Source is null)
        {
            return;
        }

        var sourceWidth = MapImage.Source.Width;
        var sourceHeight = MapImage.Source.Height;

        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return;
        }

        var scale = System.Math.Min(MapContainer.ActualWidth / sourceWidth, MapContainer.ActualHeight / sourceHeight);
        var renderedWidth = sourceWidth * scale;
        var renderedHeight = sourceHeight * scale;
        var renderedLeft = (MapContainer.ActualWidth - renderedWidth) / 2;
        var renderedTop = (MapContainer.ActualHeight - renderedHeight) / 2;

        MapOverlay.Width = MapContainer.ActualWidth;
        MapOverlay.Height = MapContainer.ActualHeight;

        var hotspotCenterX = renderedLeft + renderedWidth * GothaMapX;
        var hotspotCenterY = renderedTop + renderedHeight * GothaMapY;

        System.Windows.Controls.Canvas.SetLeft(GothaHotspotButton, hotspotCenterX - GothaHotspotButton.Width / 2);
        System.Windows.Controls.Canvas.SetTop(GothaHotspotButton, hotspotCenterY - GothaHotspotButton.Height / 2);
    }


    private void MapContainer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.IsMapCalibrationModeEnabled)
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

        viewModel.RegisterMapCalibrationPoint(relativeX, relativeY);
    }

    private void OpenCityButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.IsGothaSelected)
        {
            return;
        }

        if (_cityWindow is not null)
        {
            if (_cityWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                _cityWindow.WindowState = System.Windows.WindowState.Normal;
            }

            _cityWindow.Activate();
            return;
        }

        _cityWindow = new CityWindow
        {
            Owner = this,
            DataContext = DataContext
        };

        _cityWindow.Closed += (_, _) => _cityWindow = null;
        _cityWindow.Show();
    }

    private void OpenLogButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        OpenLogWindow();
    }

    public void OpenLogWindow()
    {
        if (_logWindow is not null)
        {
            if (_logWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                _logWindow.WindowState = System.Windows.WindowState.Normal;
            }

            _logWindow.Activate();
            return;
        }

        _logWindow = new LogWindow
        {
            Owner = this,
            DataContext = DataContext
        };

        _logWindow.Closed += (_, _) => _logWindow = null;
        _logWindow.Show();
    }
}
