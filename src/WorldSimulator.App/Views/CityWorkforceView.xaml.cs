using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.Views;

/// <summary>
/// Displays read-only workforce diagnostics for the selected city.
/// </summary>
public partial class CityWorkforceView : UserControl
{
    public CityWorkforceView()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        DataContextChanged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        RootPanel.Children.Clear();
        AddHeader("Рабочая сила города");

        if (!TryResolveWorldAndCity(out var world, out var city))
        {
            AddMutedText("Данные города недоступны.");
            return;
        }

        var capacity = world.FindSettlementSectorCapacityProfile(city.Id);
        if (capacity is null)
        {
            AddMutedText("Для выбранного города не найден профиль локальных лимитов секторов.");
            return;
        }

        var lawProfile = new WorkforceLawProfile();
        var allocation = new CityWorkforceAllocator().Allocate(city, capacity, lawProfile);

        AddSection("Население");
        AddRow("Всего жителей", city.Demographics.TotalPopulation.ToString("0"));
        AddRow("Дети", city.Demographics.Children.ToString("0"));
        AddRow("Взрослые мужчины", city.Demographics.AdultMen.ToString("0"));
        AddRow("Взрослые женщины", city.Demographics.AdultWomen.ToString("0"));
        AddRow("Старики", city.Demographics.Elderly.ToString("0"));
        foreach (var group in city.Demographics.RaceGroups)
        {
            AddRow($"Раса: {group.RaceId}", $"{group.TotalPopulation:0}", $"Дети {group.Children}, мужчины {group.AdultMen}, женщины {group.AdultWomen}, старики {group.Elderly}");
        }

        AddSeparator();
        AddSection("Трудоспособные");
        AddRow("Мужчины", allocation.Workforce.AdultMaleWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.AdultMaleWorkRate:P0}");
        AddRow("Женщины", allocation.Workforce.AdultFemaleWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.AdultFemaleWorkRate:P0}");
        AddRow("Старики", allocation.Workforce.ElderlyWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.ElderlyWorkRate:P0}");
        AddRow("Детский труд", allocation.Workforce.ChildWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.ChildLaborRate:P0}");
        AddRow("Глобальный модификатор", allocation.Workforce.GlobalWorkforceModifier.ToString("0.##"));
        AddRow("Всего рабочих", allocation.Workforce.TotalWorkers.ToString("0"));

        AddSeparator();
        AddSection("Распределение по секторам");
        AddMutedText("Preview рассчитан по текущему состоянию города. Производство меняется отдельной логикой симуляции.");
        AddRow("Земледелие", allocation.AgricultureWorkers.ToString("0"));
        AddRow("Рыбалка", allocation.FishingWorkers.ToString("0"));
        AddRow("Охота", allocation.HuntingWorkers.ToString("0"));
        AddRow("Добыча ресурсов", allocation.ResourceGatheringWorkers.ToString("0"));
        AddRow("Крафтинг", allocation.CraftingWorkers.ToString("0"));
        AddRow("Торговля", allocation.TradeWorkers.ToString("0"));
        AddRow("Стража", allocation.GuardWorkers.ToString("0"));
        AddRow("Обслуживание", allocation.MaintenanceWorkers.ToString("0"));
        AddRow("Свободные / idle", allocation.IdleWorkers.ToString("0"));

        AddSeparator();
        AddSection("Локальные лимиты секторов");
        AddRow("Земледелие", capacity.AgricultureCapacity.ToString("0"));
        AddRow("Рыбалка", capacity.FishingCapacity.ToString("0"));
        AddRow("Охота", capacity.HuntingCapacity.ToString("0"));
        AddRow("Добыча ресурсов", capacity.ResourceGatheringCapacity.ToString("0"));
        AddRow("Крафтинг", capacity.CraftingCapacity.ToString("0"));
        AddRow("Торговля", capacity.TradeCapacity.ToString("0"));
        AddRow("Стража", capacity.GuardCapacity.ToString("0"));
        AddRow("Обслуживание", capacity.MaintenanceCapacity.ToString("0"));
    }

    private bool TryResolveWorldAndCity(out SimulationWorld world, out City city)
    {
        world = null!;
        city = null!;
        var dataContext = DataContext;
        if (dataContext is null)
        {
            return false;
        }

        if (dataContext is SelectedCityViewModel selectedCity)
        {
            world = selectedCity.World;
            city = selectedCity.City;
            return true;
        }

        var type = dataContext.GetType();
        world = type.GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dataContext) as SimulationWorld ?? null!;
        city = type.GetField("_city", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dataContext) as City ?? null!;
        return world is not null && city is not null;
    }

    private void AddHeader(string text)
    {
        RootPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });
    }

    private void AddSection(string text)
    {
        RootPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
    }

    private void AddRow(string title, string value, string description = "")
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = value });
        if (!string.IsNullOrWhiteSpace(description))
        {
            panel.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap
            });
        }

        RootPanel.Children.Add(panel);
    }

    private void AddMutedText(string text)
    {
        RootPanel.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
    }

    private void AddSeparator()
    {
        RootPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 10) });
    }
}
