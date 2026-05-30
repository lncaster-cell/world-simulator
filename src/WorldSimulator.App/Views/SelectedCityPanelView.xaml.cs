using System.Windows;
using System.Windows.Controls;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Views;

/// <summary>
/// Expects <see cref="SelectedCityViewModel" /> as its DataContext.
/// </summary>
public partial class SelectedCityPanelView : UserControl
{
    public SelectedCityPanelView()
    {
        InitializeComponent();
    }

    private void OpenCityButton_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenCityWindow();
        }
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenLogWindow();
        }
    }
}
