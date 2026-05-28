using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WorldSimulator.Core.Cities;

namespace WorldSimulator.App;

public partial class CityWindow : Window
{
    public CityWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => AddInfrastructureTab();
    }

    private void AddInfrastructureTab()
    {
        if (Content is not Grid grid)
        {
            return;
        }

        var tabControl = grid.Children.OfType<TabControl>().FirstOrDefault();
        if (tabControl is null || tabControl.Items.OfType<TabItem>().Any(x => Equals(x.Header, "Инфраструктура")))
        {
            return;
        }

        var city = ResolveCityFromDataContext();
        if (city is null)
        {
            return;
        }

        tabControl.Items.Insert(Math.Max(0, tabControl.Items.Count - 1), new TabItem
        {
            Header = "Инфраструктура",
            Content = BuildInfrastructureContent(city)
        });
    }

    private City? ResolveCityFromDataContext()
    {
        var viewModel = DataContext;
        if (viewModel is null)
        {
            return null;
        }

        return viewModel.GetType()
            .GetField("_city", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(viewModel) as City;
    }

    private static UIElement BuildInfrastructureContent(City city)
    {
        var infrastructure = city.Infrastructure;
        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(new TextBlock
        {
            Text = $"Инфраструктура города {city.Name}",
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        panel.Children.Add(CreateInfrastructureRow("Жилая инфраструктура", infrastructure.HousingLevel, "Жильё, районы проживания, базовая вместимость населения."));
        panel.Children.Add(CreateInfrastructureRow("Городская инфраструктура", infrastructure.UrbanLevel, "Дороги, склады, рынки, городские службы."));
        panel.Children.Add(CreateInfrastructureRow("Производственная инфраструктура", infrastructure.ProductionLevel, "Мастерские, производственные площадки, ремесленные мощности."));
        panel.Children.Add(CreateInfrastructureRow("Военная инфраструктура", infrastructure.MilitaryLevel, "Казармы, укрепления, стража, оборонные объекты."));

        panel.Children.Add(new TextBlock
        {
            Text = "Уровни 1–5 пока являются базовым состоянием без бонусов. В будущих фазах события и развитие города смогут повышать или снижать эти уровни.",
            FontStyle = System.Windows.FontStyles.Italic,
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        });

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel
        };
    }

    private static UIElement CreateInfrastructureRow(string title, int level, string description)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(new TextBlock { Text = title, FontWeight = System.Windows.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = $"Уровень: {level}/5", Margin = new Thickness(0, 2, 0, 2) });
        panel.Children.Add(new TextBlock { Text = description, Foreground = System.Windows.Media.Brushes.DimGray, TextWrapping = TextWrapping.Wrap });
        return panel;
    }
}
