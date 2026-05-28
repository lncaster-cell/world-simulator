using System.Windows.Controls;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Views;

/// <summary>
/// Expects <see cref="MainWindowViewModel" /> as its inherited DataContext.
/// </summary>
public partial class CityInfrastructureView : UserControl
{
    public CityInfrastructureView()
    {
        InitializeComponent();
    }
}
