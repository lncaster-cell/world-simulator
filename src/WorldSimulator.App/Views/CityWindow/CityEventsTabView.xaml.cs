using System.Windows.Controls;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Views.CityWindow;

/// <summary>
/// Expects <see cref="MainWindowViewModel" /> as its explicit DataContext.
/// </summary>
public partial class CityEventsTabView : UserControl
{
    public CityEventsTabView()
    {
        InitializeComponent();
    }
}
