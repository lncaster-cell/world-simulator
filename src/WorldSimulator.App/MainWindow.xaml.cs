using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App;

public partial class MainWindow : System.Windows.Window
{
    private CityWindow? _cityWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
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
}
