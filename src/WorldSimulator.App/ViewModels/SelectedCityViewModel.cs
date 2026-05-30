using System.Windows.Input;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class SelectedCityViewModel : ViewModelBase
{
    private readonly Func<SimulationWorld> _getWorld;
    private readonly Func<City> _getCity;
    private readonly Func<DailyFoodFlowResult> _getDailyFoodFlowResult;
    private readonly Func<DailyWealthFlowResult?> _getDailyWealthFlowResult;
    private readonly Action _openCityPanel;

    public SelectedCityViewModel(
        Func<SimulationWorld> getWorld,
        Func<City> getCity,
        Func<DailyFoodFlowResult> getDailyFoodFlowResult,
        Func<DailyWealthFlowResult?> getDailyWealthFlowResult,
        Action openCityPanel)
    {
        _getWorld = getWorld;
        _getCity = getCity;
        _getDailyFoodFlowResult = getDailyFoodFlowResult;
        _getDailyWealthFlowResult = getDailyWealthFlowResult;
        _openCityPanel = openCityPanel;

        OpenSelectedCityCommand = new RelayCommand(OpenCityPanel, () => !string.IsNullOrWhiteSpace(World.SelectedCityId));
    }

    public ICommand OpenSelectedCityCommand { get; }

    public SimulationWorld World => _getWorld();

    public City City => _getCity();

    public new string SelectedCityName => City.Name;

    public new string SelectedRegionName => World.SelectedRegion.DisplayName;

    public new string CityName => City.Name;

    public string SelectedCityProfile => $"{City.Name} — профиль поселения";

    public new CityState CityState => City.CityState;

    public new string CityStateDisplay => ToRussianCityState(CityState);

    public new int Population => City.Population;

    public new decimal Food => City.Food;

    public new string FoodDisplay => FormatOneDecimal(Food);

    public new decimal Resources => City.Resources;

    public new string ResourcesDisplay => FormatOneDecimal(Resources);

    public new decimal Goods => City.Goods;

    public new string GoodsDisplay => FormatOneDecimal(Goods);

    public new decimal Wealth => City.Wealth;

    public new string WealthDisplay => FormatOneDecimal(Wealth);

    public new int Mood => City.Mood;

    public new int Security => City.Security;

    public new int Crime => City.Crime;

    public new decimal DailyFoodConsumption => City.CalculateDailyFoodConsumption();

    public new IReadOnlyList<CityInfrastructureRowViewModel> CityInfrastructureRows
    {
        get
        {
            var infrastructure = City.Infrastructure;
            return
            [
                new CityInfrastructureRowViewModel("Жилая инфраструктура", infrastructure.HousingLevel, "Жильё, дворы, бытовые постройки и вместимость поселения."),
                new CityInfrastructureRowViewModel("Городская инфраструктура", infrastructure.UrbanLevel, "Улицы, склады, колодцы, административные и общественные объекты."),
                new CityInfrastructureRowViewModel("Производственная инфраструктура", infrastructure.ProductionLevel, "Мастерские, поля, мельницы, добывающие и ремесленные объекты."),
                new CityInfrastructureRowViewModel("Военная инфраструктура", infrastructure.MilitaryLevel, "Стены, казармы, посты стражи и оборонительные объекты.")
            ];
        }
    }

    public new decimal DailyFoodStartingFood => DailyFoodResult.StartingFood;
    public new decimal DailyFoodPopulationConsumption => DailyFoodResult.PopulationConsumption;
    public new decimal DailyFoodAgricultureIncome => DailyFoodResult.AgricultureIncome;
    public new decimal DailyFoodFishingIncome => DailyFoodResult.FishingIncome;
    public new decimal DailyFoodHuntingIncome => DailyFoodResult.HuntingIncome;
    public new decimal DailyFoodMainlandSupplyIncome => DailyFoodResult.MainlandSupplyIncome;
    public new decimal DailyFoodEventDelta => DailyFoodResult.EventDelta;
    public new decimal DailyFoodTotalDelta => DailyFoodResult.TotalDelta;
    public new decimal DailyFoodEndingFood => DailyFoodResult.EndingFood;

    public new string DailyFoodPopulationConsumptionDisplay => $"-{FormatOneDecimal(DailyFoodPopulationConsumption)}";
    public new string DailyFoodAgricultureIncomeDisplay => FormatSigned(DailyFoodAgricultureIncome);
    public new string DailyFoodFishingIncomeDisplay => FormatSigned(DailyFoodFishingIncome);
    public new string DailyFoodHuntingIncomeDisplay => FormatSigned(DailyFoodHuntingIncome);
    public new string DailyFoodMainlandSupplyIncomeDisplay => FormatSigned(DailyFoodMainlandSupplyIncome);
    public new string DailyFoodEventDeltaDisplay => FormatSigned(DailyFoodEventDelta);
    public new string DailyFoodTotalDeltaDisplay => FormatSigned(DailyFoodTotalDelta);

    public new string FoodBalanceTooltip =>
        $"Дневной баланс пищи:{Environment.NewLine}" +
        $"Начало дня: {FormatOneDecimal(DailyFoodStartingFood)}{Environment.NewLine}" +
        $"Потребление: -{FormatOneDecimal(DailyFoodPopulationConsumption)}{Environment.NewLine}" +
        $"Земледелие: {FormatSigned(DailyFoodAgricultureIncome)}{Environment.NewLine}" +
        $"Рыбалка: {FormatSigned(DailyFoodFishingIncome)}{Environment.NewLine}" +
        $"Охота: {FormatSigned(DailyFoodHuntingIncome)}{Environment.NewLine}" +
        $"Поставки: {FormatSigned(DailyFoodMainlandSupplyIncome)}{Environment.NewLine}" +
        $"События: {FormatSigned(DailyFoodEventDelta)}{Environment.NewLine}" +
        $"Итог: {FormatSigned(DailyFoodTotalDelta)}";

    public new string FishingProductionTooltip => "Рыбалка зависит от локального профиля поселения, работников сектора и текущих модификаторов.";

    public new string ResourcesTooltip => $"Ресурсы: {ResourcesDisplay}";

    public new string GoodsTooltip => $"Товары: {GoodsDisplay}";

    public new string CrimeFlowTooltip => "Преступность пересчитывается недельным шагом симуляции.";

    public new string WealthTooltip => BuildWealthTooltip();

    public string EconomyStocksTooltip => $"{ResourcesTooltip}{Environment.NewLine}{Environment.NewLine}{GoodsTooltip}";

    public void RefreshSelectedCity()
    {
        OnPropertyChanged(nameof(SelectedCityName));
        OnPropertyChanged(nameof(SelectedRegionName));
        OnPropertyChanged(nameof(CityName));
        OnPropertyChanged(nameof(SelectedCityProfile));
        OnPropertyChanged(nameof(CityInfrastructureRows));
        OnPropertyChanged(nameof(CityState));
        OnPropertyChanged(nameof(CityStateDisplay));
        OnPropertyChanged(nameof(Population));
        RefreshCityStocksAndFlows();
        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(CrimeFlowTooltip));
        RaiseOpenCommandCanExecuteChanged();
    }

    public void RefreshCityStocksAndFlows()
    {
        OnPropertyChanged(nameof(Food));
        OnPropertyChanged(nameof(FoodDisplay));
        OnPropertyChanged(nameof(Resources));
        OnPropertyChanged(nameof(ResourcesDisplay));
        OnPropertyChanged(nameof(Goods));
        OnPropertyChanged(nameof(GoodsDisplay));
        OnPropertyChanged(nameof(Wealth));
        OnPropertyChanged(nameof(WealthDisplay));
        OnPropertyChanged(nameof(DailyFoodConsumption));
        RefreshFlowProperties();
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
        OnPropertyChanged(nameof(EconomyStocksTooltip));
        OnPropertyChanged(nameof(WealthTooltip));
    }

    public void RefreshFlowProperties()
    {
        OnPropertyChanged(nameof(DailyFoodStartingFood));
        OnPropertyChanged(nameof(DailyFoodPopulationConsumption));
        OnPropertyChanged(nameof(DailyFoodAgricultureIncome));
        OnPropertyChanged(nameof(DailyFoodFishingIncome));
        OnPropertyChanged(nameof(DailyFoodHuntingIncome));
        OnPropertyChanged(nameof(DailyFoodMainlandSupplyIncome));
        OnPropertyChanged(nameof(DailyFoodEventDelta));
        OnPropertyChanged(nameof(DailyFoodTotalDelta));
        OnPropertyChanged(nameof(DailyFoodEndingFood));
        OnPropertyChanged(nameof(DailyFoodPopulationConsumptionDisplay));
        OnPropertyChanged(nameof(DailyFoodAgricultureIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodFishingIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodHuntingIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodMainlandSupplyIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodEventDeltaDisplay));
        OnPropertyChanged(nameof(DailyFoodTotalDeltaDisplay));
        OnPropertyChanged(nameof(FoodBalanceTooltip));
        OnPropertyChanged(nameof(FishingProductionTooltip));
    }

    public void RefreshCityState()
    {
        OnPropertyChanged(nameof(CityState));
        OnPropertyChanged(nameof(CityStateDisplay));
        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(FishingProductionTooltip));
        OnPropertyChanged(nameof(WealthTooltip));
    }

    public void RaiseOpenCommandCanExecuteChanged()
    {
        if (OpenSelectedCityCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private DailyFoodFlowResult DailyFoodResult => _getDailyFoodFlowResult();

    private void OpenCityPanel() => _openCityPanel();

    private string BuildWealthTooltip()
    {
        var flow = _getDailyWealthFlowResult() ?? new DailyWealthFlowResult
        {
            StartingWealth = Wealth,
            PortTradeBonus = 0m,
            GoodsProductionBonus = 0m,
            ConsumptionCoverageBonus = 0m,
            FoodShortagePenalty = 0m,
            GoodsShortagePenalty = 0m,
            ResourcesShortagePenalty = 0m,
            SecurityModifierDelta = 0m,
            CrimePenalty = 0m,
            CityStateDelta = 0m,
            TotalDelta = 0m,
            EndingWealth = Wealth
        };

        return $"Благосостояние:{Environment.NewLine}" +
               $"Текущее значение: {Wealth:0.##}{Environment.NewLine}{Environment.NewLine}" +
               $"Прогноз на день:{Environment.NewLine}" +
               $"Портовая торговля: {flow.PortTradeBonus:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Производство товаров: {flow.GoodsProductionBonus:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Покрытие бытовых потребностей: {flow.ConsumptionCoverageBonus:+0.##;-0.##;0}{Environment.NewLine}{Environment.NewLine}" +
               $"Штрафы:{Environment.NewLine}" +
               $"Нехватка еды: {flow.FoodShortagePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Дефицит товаров: {flow.GoodsShortagePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Дефицит ресурсов: {flow.ResourcesShortagePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Безопасность: {flow.SecurityModifierDelta:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Преступность: {flow.CrimePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Состояние города: {flow.CityStateDelta:+0.##;-0.##;0}{Environment.NewLine}{Environment.NewLine}" +
               $"Итоговый баланс: {flow.TotalDelta:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Ожидаемое благосостояние после дня: {flow.EndingWealth:0.##}";
    }

    private static string FormatOneDecimal(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.#");

    private static string FormatSigned(decimal value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return rounded.ToString("+0.#;-0.#;0");
    }

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
