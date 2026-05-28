using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class TradeRoutePresetTests
{
    [Fact]
    public void DefaultRoutes_AllEndpointsExistAmongRiviaSettlementsAndHaveCoordinates()
    {
        var settlementIds = RiviaSettlementPresets.All
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var routes = TradeRoutePresets.CreateDefaultRoutes();

        routes.Should().OnlyContain(route =>
            EndpointExistsAndHasCoordinates(route.FromSettlementId, settlementIds) &&
            EndpointExistsAndHasCoordinates(route.ToSettlementId, settlementIds));
    }

    private static bool EndpointExistsAndHasCoordinates(string settlementId, IReadOnlySet<string> settlementIds)
    {
        if (!settlementIds.Contains(settlementId))
        {
            return false;
        }

        if (!RiviaSettlementPresets.TryGet(settlementId, out var preset))
        {
            return false;
        }

        return preset.X is >= 0m and <= 1m && preset.Y is >= 0m and <= 1m;
    }
}
