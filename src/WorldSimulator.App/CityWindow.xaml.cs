namespace WorldSimulator.App;

public partial class CityWindow : System.Windows.Window
{
    public CityWindow()
    {
        InitializeComponent();
    }

    private void OpenLogButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (Owner is MainWindow mainWindow)
        {
            mainWindow.OpenLogWindow();
        }
    }
}
