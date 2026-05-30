using WorldSimulator.Core.Cities;

namespace WorldSimulator.App.Services;

public static class CityStateTextFormatter
{
    public static string ToRussian(CityState cityState)
    {
        return cityState switch
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
}
