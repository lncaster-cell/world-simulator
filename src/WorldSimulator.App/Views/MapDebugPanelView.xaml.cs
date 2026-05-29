using System.Windows.Controls;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App.Views;

/// <summary>
/// Expects <see cref="MapViewModel" /> as its DataContext.
/// </summary>
public partial class MapDebugPanelView : UserControl
{
    public MapDebugPanelView()
    {
        InitializeComponent();
    }
}
