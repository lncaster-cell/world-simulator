using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App;

public partial class MainWindow : System.Windows.Window
{
    private CityWindow? _cityWindow;
    private LogWindow? _logWindow;

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
