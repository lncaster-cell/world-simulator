using System.Globalization;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.Services;

public sealed class TradeRouteSelectionService
{
    public const int MaxDestinationCount = 8;

    public TradeRouteSelectionResult ResolveSelection(SimulationWorld world, City origin, City destination)
    {
        var existingRoute = FindRouteBetween(world, origin, destination);
        if (existingRoute is not null)
        {
            return new TradeRouteSelectionResult(
                existingRoute,
                $"RouteId: {existingRoute.Id}",
                existingRoute.DistanceDays,
                FormatDistanceDaysInput(existingRoute.DistanceDays),
                ShouldClearEditedPoints: false);
        }

        return new TradeRouteSelectionResult(
            SelectedRoute: null,
            RouteIdDisplay: $"Новый маршрут: {origin.Id}_{destination.Id}",
            DistanceDays: 1m,
            DistanceDaysInput: "1.0",
            ShouldClearEditedPoints: true);
    }

    public string FormatDistanceDaysInput(decimal distanceDays)
        => distanceDays.ToString("0.0###", CultureInfo.InvariantCulture);

    public TradeRouteDestinationAddResult TryAddDestination(
        City? origin,
        City? destination,
        IEnumerable<City> selectedDestinations)
    {
        if (origin is null)
        {
            return TradeRouteDestinationAddResult.Failure("Сначала выберите пункт отправления.");
        }

        if (destination is null)
        {
            return TradeRouteDestinationAddResult.NoOp;
        }

        if (destination.Id == origin.Id)
        {
            return TradeRouteDestinationAddResult.Failure("Пункт назначения не может совпадать с пунктом отправления.");
        }

        var selectedDestinationList = selectedDestinations as IReadOnlyCollection<City> ?? selectedDestinations.ToList();
        if (selectedDestinationList.Any(x => x.Id == destination.Id))
        {
            return TradeRouteDestinationAddResult.Failure("Пункт назначения уже добавлен.");
        }

        if (selectedDestinationList.Count >= MaxDestinationCount)
        {
            return TradeRouteDestinationAddResult.Failure("Можно выбрать максимум 8 пунктов назначения.");
        }

        return TradeRouteDestinationAddResult.Success(destination);
    }

    public TradeRouteSaveInputResult ResolveSaveInput(City? origin, City? destination, string distanceDaysInput)
    {
        if (origin is null || destination is null)
        {
            return TradeRouteSaveInputResult.Failure("Маршрут не сохранён: выберите пункт отправления и активный пункт назначения.");
        }

        if (origin.Id == destination.Id)
        {
            return TradeRouteSaveInputResult.Failure("Маршрут не сохранён: отправление и назначение совпадают.");
        }

        if (!decimal.TryParse(distanceDaysInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var distanceDays))
        {
            return TradeRouteSaveInputResult.Failure("Маршрут не сохранён: укажите корректное значение дней пути.");
        }

        if (distanceDays < 0.1m)
        {
            return TradeRouteSaveInputResult.Failure("Маршрут не сохранён: дней пути должно быть не меньше 0.1.");
        }

        return TradeRouteSaveInputResult.Success(origin, destination, distanceDays);
    }

    public static TradeRoute? FindRouteBetween(SimulationWorld world, City from, City to) => world.TradeRoutes.FirstOrDefault(route =>
        (route.FromSettlementId == from.Id && route.ToSettlementId == to.Id)
        || (route.FromSettlementId == to.Id && route.ToSettlementId == from.Id));
}

public sealed record TradeRouteSelectionResult(
    TradeRoute? SelectedRoute,
    string RouteIdDisplay,
    decimal DistanceDays,
    string DistanceDaysInput,
    bool ShouldClearEditedPoints);

public sealed record TradeRouteDestinationAddResult(bool ShouldAdd, City? Destination, string? ErrorMessage)
{
    public static TradeRouteDestinationAddResult NoOp { get; } = new(false, null, null);

    public static TradeRouteDestinationAddResult Success(City destination) => new(true, destination, null);

    public static TradeRouteDestinationAddResult Failure(string errorMessage) => new(false, null, errorMessage);
}

public sealed record TradeRouteSaveInputResult(bool IsValid, City? Origin, City? Destination, decimal DistanceDays, string? ErrorMessage)
{
    public static TradeRouteSaveInputResult Success(City origin, City destination, decimal distanceDays)
        => new(true, origin, destination, distanceDays, null);

    public static TradeRouteSaveInputResult Failure(string errorMessage)
        => new(false, null, null, 0m, errorMessage);
}
