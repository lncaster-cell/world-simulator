using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool _isCityPanelVisible;
    private int _selectedCityTabIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> TechnicalLogEntries { get; } = new();
    public ObservableCollection<string> ActiveEventEntries { get; } = new();
    public ObservableCollection<string> CompletedEventEntries { get; } = new();

    public bool HasTechnicalLogEntries => TechnicalLogEntries.Count > 0;
    public bool HasActiveEventEntries => ActiveEventEntries.Count > 0;
    public bool HasCompletedEventEntries => CompletedEventEntries.Count > 0;

    public bool IsCityPanelVisible
    {
        get => _isCityPanelVisible;
        set
        {
            if (_isCityPanelVisible == value)
            {
                return;
            }

            _isCityPanelVisible = value;
            OnPropertyChanged();
        }
    }

    public int SelectedCityTabIndex
    {
        get => _selectedCityTabIndex;
        set
        {
            if (_selectedCityTabIndex == value)
            {
                return;
            }

            _selectedCityTabIndex = value;
            OnPropertyChanged();
        }
    }

    public string SelectedCityName => CurrentCity?.Name ?? "—";
    public string SelectedRegionName => CurrentWorld?.SelectedRegion.DisplayName ?? "—";
    public string CityName => CurrentCity?.Name ?? "—";
    public CityState CityState => CurrentCity?.CityState ?? CityState.Stable;
    public string CityStateDisplay => ToRussianCityState(CityState);
    public int Population => CurrentCity?.Population ?? 0;
    public decimal Food => CurrentCity?.Food ?? 0m;
    public decimal Wealth => CurrentCity?.Wealth ?? 0m;
    public int Mood => CurrentCity?.Mood ?? 0;
    public int Security => CurrentCity?.Security ?? 0;
    public int Crime => CurrentCity?.Crime ?? 0;
    public decimal Resources => CurrentCity?.Resources ?? 0m;
    public decimal Goods => CurrentCity?.Goods ?? 0m;
    public decimal DailyFoodConsumption => CurrentCity?.CalculateDailyFoodConsumption() ?? 0m;
    public IReadOnlyList<TradeRoute> TradeRoutes => CurrentWorld?.TradeRoutes is IReadOnlyList<TradeRoute> routes
        ? routes
        : Array.Empty<TradeRoute>();

    public IReadOnlyList<CityInfrastructureRowViewModel> CityInfrastructureRows
    {
        get
        {
            var infrastructure = CurrentCity?.Infrastructure ?? new CityInfrastructure();
            return
            [
                new CityInfrastructureRowViewModel("Жилая инфраструктура", infrastructure.HousingLevel, "Жильё, дворы, бытовые постройки и вместимость поселения."),
                new CityInfrastructureRowViewModel("Городская инфраструктура", infrastructure.UrbanLevel, "Улицы, склады, колодцы, административные и общественные объекты."),
                new CityInfrastructureRowViewModel("Производственная инфраструктура", infrastructure.ProductionLevel, "Мастерские, поля, мельницы, добывающие и ремесленные объекты."),
                new CityInfrastructureRowViewModel("Военная инфраструктура", infrastructure.MilitaryLevel, "Стены, казармы, посты стражи и оборонительные объекты.")
            ];
        }
    }

    public decimal DailyFoodStartingFood => DailyFoodResult.StartingFood;
    public decimal DailyFoodPopulationConsumption => DailyFoodResult.PopulationConsumption;
    public decimal DailyFoodAgricultureIncome => DailyFoodResult.AgricultureIncome;
    public decimal DailyFoodFishingIncome => DailyFoodResult.FishingIncome;
    public decimal DailyFoodHuntingIncome => DailyFoodResult.HuntingIncome;
    public decimal DailyFoodMainlandSupplyIncome => DailyFoodResult.MainlandSupplyIncome;
    public decimal DailyFoodEventDelta => DailyFoodResult.EventDelta;
    public decimal DailyFoodTotalDelta => DailyFoodResult.TotalDelta;
    public decimal DailyFoodEndingFood => DailyFoodResult.EndingFood;

    public string DailyFoodPopulationConsumptionDisplay => $"-{DailyFoodPopulationConsumption:0.##}";
    public string DailyFoodAgricultureIncomeDisplay => FormatSigned(DailyFoodAgricultureIncome);
    public string DailyFoodFishingIncomeDisplay => FormatSigned(DailyFoodFishingIncome);
    public string DailyFoodHuntingIncomeDisplay => FormatSigned(DailyFoodHuntingIncome);
    public string DailyFoodMainlandSupplyIncomeDisplay => FormatSigned(DailyFoodMainlandSupplyIncome);
    public string DailyFoodEventDeltaDisplay => FormatSigned(DailyFoodEventDelta);
    public string DailyFoodTotalDeltaDisplay => FormatSigned(DailyFoodTotalDelta);

    public string FoodBalanceTooltip =>
        $"Пищевой баланс:{Environment.NewLine}" +
        $"Начало дня: {DailyFoodStartingFood:0.##}{Environment.NewLine}" +
        $"Потребление: -{DailyFoodPopulationConsumption:0.##}{Environment.NewLine}" +
        $"Земледелие: {FormatSigned(DailyFoodAgricultureIncome)}{Environment.NewLine}" +
        $"Рыбалка: {FormatSigned(DailyFoodFishingIncome)}{Environment.NewLine}" +
        $"Охота: {FormatSigned(DailyFoodHuntingIncome)}{Environment.NewLine}" +
        $"Поставки: {FormatSigned(DailyFoodMainlandSupplyIncome)}{Environment.NewLine}" +
        $"События: {FormatSigned(DailyFoodEventDelta)}{Environment.NewLine}" +
        $"Итог: {FormatSigned(DailyFoodTotalDelta)}";

    public string FishingProductionTooltip => "Рыбалка зависит от локального профиля поселения, работников сектора и текущих модификаторов.";
    public string ResourcesTooltip => $"Ресурсы: {Resources:0.##}";
    public string GoodsTooltip => $"Товары: {Goods:0.##}";
    public string CrimeFlowTooltip => "Преступность пересчитывается недельным шагом симуляции.";
    public string WealthTooltip => BuildWealthTooltipFallback();

    public string SimulationSummaryDayAndHour => $"День {GetPublicProperty<int>("Day")}, час {GetPublicProperty<int>("Hour")}";
    public string SimulationSummaryCityState => $"Состояние: {CityStateDisplay}";
    public string SimulationSummaryFoodBalance => $"Пища: {Food:0.##} ({FormatSigned(DailyFoodTotalDelta)}/день)";
    public string SimulationSummaryActiveEvents => $"Активных событий: {ActiveEventEntries.Count}";
    public string SimulationSummaryRandomEventsStatus => GetPublicProperty<bool>("IsRandomEventGenerationEnabled")
        ? "Случайные события: включены"
        : "Случайные события: выключены";
    public string SimulationSummaryLastImportantChange => $"Последнее важное изменение: {GetPrivateField<string>("_lastImportantChange") ?? "пока нет."}";

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private SimulationWorld? CurrentWorld => GetPrivateField<SimulationWorld>("_world");
    private City? CurrentCity => GetPrivateField<City>("_city");

    private DailyFoodFlowResult DailyFoodResult => GetPrivateField<DailyFoodFlowResult>("_dailyFoodFlowResult") ?? new DailyFoodFlowResult
    {
        StartingFood = Food,
        PopulationConsumption = DailyFoodConsumption,
        AgricultureIncome = 0m,
        FishingIncome = 0m,
        HuntingIncome = 0m,
        MainlandSupplyIncome = 0m,
        EventDelta = 0m,
        TotalDelta = -DailyFoodConsumption,
        EndingFood = Math.Max(0m, Food - DailyFoodConsumption)
    };

    private string BuildWealthTooltipFallback()
    {
        var flow = GetPrivateField<DailyWealthFlowResult>("_dailyWealthFlowResult");
        if (flow is null)
        {
            return $"Благосостояние: {Wealth:0.##}";
        }

        return $"Благосостояние:{Environment.NewLine}" +
               $"Текущее значение: {Wealth:0.##}{Environment.NewLine}" +
               $"Итоговый баланс: {FormatSigned(flow.TotalDelta)}{Environment.NewLine}" +
               $"Ожидаемое благосостояние после дня: {flow.EndingWealth:0.##}";
    }

    private T? GetPrivateField<T>(string name)
    {
        var field = GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(this) is T value)
        {
            return value;
        }

        return default;
    }

    private T GetPublicProperty<T>(string name)
    {
        var property = GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.GetValue(this) is T value)
        {
            return value;
        }

        return default!;
    }

    private static string FormatSigned(decimal value) => value.ToString("+0.##;-0.##;0");

    private static string ToRussianCityState(CityState cityState) => cityState switch
    {
        CityState.Stable => "Стабильность",
        CityState.Prosperous => "Процветание",
        CityState.Stagnation => "Стагнация",
        CityState.FoodShortage => "Нехватка пищи",
        CityState.Famine => "Голод",
        CityState.EconomicDecline => "Экономический спад",
        CityState.CrimeProblem => "Проблемы с преступностью",
        CityState.Unrest => "Беспорядки",
        CityState.Recovery => "Восстановление",
        CityState.Collapse => "Коллапс",
        CityState.Abandoned => "Опустевший город",
        _ => cityState.ToString()
    };
}
