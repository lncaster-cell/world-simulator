using System;
using System.Collections.Generic;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class SelectedCityViewModel : ViewModelBase
{
    private readonly Func<SimulationWorld> _worldProvider;
    private readonly Func<City> _cityProvider;
    private readonly Func<DailyFoodFlowResult> _dailyFoodFlowProvider;
    private readonly Func<DailyWealthFlowResult?> _dailyWealthFlowProvider;

    public SelectedCityViewModel(
        Func<SimulationWorld> worldProvider,
        Func<City> cityProvider,
        Func<DailyFoodFlowResult> dailyFoodFlowProvider,
        Func<DailyWealthFlowResult?> dailyWealthFlowProvider)
    {
        _worldProvider = worldProvider;
        _cityProvider = cityProvider;
        _dailyFoodFlowProvider = dailyFoodFlowProvider;
        _dailyWealthFlowProvider = dailyWealthFlowProvider;
    }

    public string SelectedCityName => CurrentCity.Name;
    public string SelectedRegionName => CurrentWorld.SelectedRegion.DisplayName;
    public string CityName => CurrentCity.Name;
    public string SelectedCityProfile => $"{CurrentCity.Name} — профиль поселения";
    public CityState CityState => CurrentCity.CityState;
    public string CityStateDisplay => ToRussianCityState(CityState);
    public int Population => CurrentCity.Population;
    public decimal Food => CurrentCity.Food;
    public decimal Wealth => CurrentCity.Wealth;
    public int Mood => CurrentCity.Mood;
    public int Security => CurrentCity.Security;
    public int Crime => CurrentCity.Crime;
    public decimal Resources => CurrentCity.Resources;
    public decimal Goods => CurrentCity.Goods;
    public string FoodDisplay => FormatOneDecimal(Food);
    public string WealthDisplay => FormatOneDecimal(Wealth);
    public string ResourcesDisplay => FormatOneDecimal(Resources);
    public string GoodsDisplay => FormatOneDecimal(Goods);
    public decimal DailyFoodConsumption => CurrentCity.CalculateDailyFoodConsumption();
    public IReadOnlyList<TradeRoute> TradeRoutes => CurrentWorld.TradeRoutes;

    public IReadOnlyList<CityInfrastructureRowViewModel> CityInfrastructureRows
    {
        get
        {
            var infrastructure = CurrentCity.Infrastructure;
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

    public string DailyFoodPopulationConsumptionDisplay => $"-{FormatOneDecimal(DailyFoodPopulationConsumption)}";
    public string DailyFoodAgricultureIncomeDisplay => FormatSigned(DailyFoodAgricultureIncome);
    public string DailyFoodFishingIncomeDisplay => FormatSigned(DailyFoodFishingIncome);
    public string DailyFoodHuntingIncomeDisplay => FormatSigned(DailyFoodHuntingIncome);
    public string DailyFoodMainlandSupplyIncomeDisplay => FormatSigned(DailyFoodMainlandSupplyIncome);
    public string DailyFoodEventDeltaDisplay => FormatSigned(DailyFoodEventDelta);
    public string DailyFoodTotalDeltaDisplay => FormatSigned(DailyFoodTotalDelta);

    public string FoodBalanceTooltip =>
        $"Пищевой баланс:{Environment.NewLine}" +
        $"Начало дня: {FormatOneDecimal(DailyFoodStartingFood)}{Environment.NewLine}" +
        $"Потребление: -{FormatOneDecimal(DailyFoodPopulationConsumption)}{Environment.NewLine}" +
        $"Земледелие: {FormatSigned(DailyFoodAgricultureIncome)}{Environment.NewLine}" +
        $"Рыбалка: {FormatSigned(DailyFoodFishingIncome)}{Environment.NewLine}" +
        $"Охота: {FormatSigned(DailyFoodHuntingIncome)}{Environment.NewLine}" +
        $"Поставки: {FormatSigned(DailyFoodMainlandSupplyIncome)}{Environment.NewLine}" +
        $"События: {FormatSigned(DailyFoodEventDelta)}{Environment.NewLine}" +
        $"Итог: {FormatSigned(DailyFoodTotalDelta)}";

    public string FishingProductionTooltip => "Рыбалка зависит от локального профиля поселения, работников сектора и текущих модификаторов.";
    public string ResourcesTooltip => $"Ресурсы: {ResourcesDisplay}";
    public string GoodsTooltip => $"Товары: {GoodsDisplay}";
    public string CrimeFlowTooltip => "Преступность пересчитывается недельным шагом симуляции.";
    public string WealthTooltip => BuildWealthTooltip();
    public string EconomyStocksTooltip => $"{ResourcesTooltip}{Environment.NewLine}{Environment.NewLine}{GoodsTooltip}";

    public void RefreshSelectedCityPanel()
    {
        OnPropertyChanged(nameof(SelectedCityName));
        OnPropertyChanged(nameof(SelectedRegionName));
        OnPropertyChanged(nameof(SelectedCityProfile));
        OnPropertyChanged(nameof(CityStateDisplay));
    }

    public void RefreshAllCityProperties()
    {
        OnPropertyChanged(nameof(CityName));
        OnPropertyChanged(nameof(SelectedCityName));
        OnPropertyChanged(nameof(SelectedRegionName));
        OnPropertyChanged(nameof(SelectedCityProfile));
        OnPropertyChanged(nameof(CityInfrastructureRows));
        OnPropertyChanged(nameof(CityState));
        OnPropertyChanged(nameof(CityStateDisplay));
        OnPropertyChanged(nameof(Population));
        OnPropertyChanged(nameof(Food));
        OnPropertyChanged(nameof(FoodDisplay));
        OnPropertyChanged(nameof(FoodBalanceTooltip));
        OnPropertyChanged(nameof(FishingProductionTooltip));
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
        OnPropertyChanged(nameof(EconomyStocksTooltip));
        OnPropertyChanged(nameof(Wealth));
        OnPropertyChanged(nameof(WealthDisplay));
        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(CrimeFlowTooltip));
        OnPropertyChanged(nameof(Resources));
        OnPropertyChanged(nameof(ResourcesDisplay));
        OnPropertyChanged(nameof(Goods));
        OnPropertyChanged(nameof(GoodsDisplay));
        OnPropertyChanged(nameof(DailyFoodConsumption));
        OnPropertyChanged(nameof(WealthTooltip));
    }

    public void RefreshDailyFoodFlowPreview()
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
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
        OnPropertyChanged(nameof(EconomyStocksTooltip));
    }

    public void RefreshTradeRoutes()
    {
        OnPropertyChanged(nameof(TradeRoutes));
    }

    private City CurrentCity => _cityProvider();
    private SimulationWorld CurrentWorld => _worldProvider();

    private DailyFoodFlowResult DailyFoodResult => _dailyFoodFlowProvider();

    private string BuildWealthTooltip()
    {
        var flow = _dailyWealthFlowProvider() ?? new DailyWealthFlowResult
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
               $"Текущее значение: {FormatOneDecimal(Wealth)}{Environment.NewLine}{Environment.NewLine}" +
               $"Прогноз на день:{Environment.NewLine}" +
               $"Портовая торговля: {FormatSigned(flow.PortTradeBonus)}{Environment.NewLine}" +
               $"Производство товаров: {FormatSigned(flow.GoodsProductionBonus)}{Environment.NewLine}" +
               $"Покрытие бытовых потребностей: {FormatSigned(flow.ConsumptionCoverageBonus)}{Environment.NewLine}{Environment.NewLine}" +
               $"Штрафы:{Environment.NewLine}" +
               $"Нехватка еды: {FormatSigned(flow.FoodShortagePenalty)}{Environment.NewLine}" +
               $"Дефицит товаров: {FormatSigned(flow.GoodsShortagePenalty)}{Environment.NewLine}" +
               $"Дефицит ресурсов: {FormatSigned(flow.ResourcesShortagePenalty)}{Environment.NewLine}" +
               $"Безопасность: {FormatSigned(flow.SecurityModifierDelta)}{Environment.NewLine}" +
               $"Преступность: {FormatSigned(flow.CrimePenalty)}{Environment.NewLine}" +
               $"Состояние города: {FormatSigned(flow.CityStateDelta)}{Environment.NewLine}{Environment.NewLine}" +
               $"Итоговый баланс: {FormatSigned(flow.TotalDelta)}{Environment.NewLine}" +
               $"Ожидаемое благосостояние после дня: {FormatOneDecimal(flow.EndingWealth)}";
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
