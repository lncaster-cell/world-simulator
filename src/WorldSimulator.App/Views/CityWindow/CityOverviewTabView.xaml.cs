using System.Windows.Controls;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Views.CityWindow;

/// <summary>
/// Expects <see cref="MainWindowViewModel" /> as its explicit DataContext.
/// </summary>
public partial class CityOverviewTabView : UserControl
{
    public CityOverviewTabView()
    {
        InitializeComponent();
    }
}
