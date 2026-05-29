using System.Text.Json.Serialization;
using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.World;

public sealed record RiviaSettlementPresetData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("route_node_id")]
    public required string RouteNodeId { get; init; }

    [JsonPropertyName("x")]
    public decimal X { get; init; }

    [JsonPropertyName("y")]
    public decimal Y { get; init; }

    [JsonPropertyName("city_start")]
    public required CityStartPresetData CityStart { get; init; }

    [JsonPropertyName("economy")]
    public required SettlementEconomyPresetData Economy { get; init; }

    [JsonPropertyName("sector_capacity")]
    public required SettlementSectorCapacityPresetData SectorCapacity { get; init; }

    [JsonPropertyName("is_port")]
    public bool IsPort { get; init; }

    [JsonPropertyName("is_fortress")]
    public bool IsFortress { get; init; }

    [JsonPropertyName("is_capital")]
    public bool IsCapital { get; init; }

    public RiviaSettlementPreset ToPreset() => new(
        Id: Id,
        DisplayName: DisplayName,
        RouteNodeId: RouteNodeId,
        X: X,
        Y: Y,
        CityStart: CityStart.ToPreset(),
        Economy: Economy.ToPreset(),
        SectorCapacity: SectorCapacity.ToPreset(),
        IsPort: IsPort,
        IsFortress: IsFortress,
        IsCapital: IsCapital);
}

public sealed record CityStartPresetData
{
    [JsonPropertyName("population")]
    public int Population { get; init; }

    [JsonPropertyName("food")]
    public decimal Food { get; init; }

    [JsonPropertyName("wealth")]
    public decimal Wealth { get; init; }

    [JsonPropertyName("mood")]
    public int Mood { get; init; }

    [JsonPropertyName("security")]
    public int Security { get; init; }

    [JsonPropertyName("crime")]
    public int Crime { get; init; }

    [JsonPropertyName("resources")]
    public decimal Resources { get; init; }

    [JsonPropertyName("goods")]
    public decimal Goods { get; init; }

    [JsonPropertyName("city_state")]
    public CityState CityState { get; init; }

    public CityStartPreset ToPreset() => new(Population, Food, Wealth, Mood, Security, Crime, Resources, Goods, CityState);
}

public sealed record SettlementEconomyPresetData
{
    [JsonPropertyName("agriculture_potential")]
    public decimal AgriculturePotential { get; init; }

    [JsonPropertyName("fishing_multiplier")]
    public decimal FishingMultiplier { get; init; }

    [JsonPropertyName("hunting_multiplier")]
    public decimal HuntingMultiplier { get; init; }

    [JsonPropertyName("mainland_supply_multiplier")]
    public decimal MainlandSupplyMultiplier { get; init; }

    [JsonPropertyName("resource_gathering_multiplier")]
    public decimal ResourceGatheringMultiplier { get; init; }

    [JsonPropertyName("goods_crafting_multiplier")]
    public decimal GoodsCraftingMultiplier { get; init; }

    public SettlementEconomyPreset ToPreset() => new(
        AgriculturePotential,
        FishingMultiplier,
        HuntingMultiplier,
        MainlandSupplyMultiplier,
        ResourceGatheringMultiplier,
        GoodsCraftingMultiplier);
}

public sealed record SettlementSectorCapacityPresetData
{
    [JsonPropertyName("agriculture_capacity")]
    public int AgricultureCapacity { get; init; }

    [JsonPropertyName("fishing_capacity")]
    public int FishingCapacity { get; init; }

    [JsonPropertyName("hunting_capacity")]
    public int HuntingCapacity { get; init; }

    [JsonPropertyName("resource_gathering_capacity")]
    public int ResourceGatheringCapacity { get; init; }

    [JsonPropertyName("crafting_capacity")]
    public int CraftingCapacity { get; init; }

    [JsonPropertyName("trade_capacity")]
    public int TradeCapacity { get; init; }

    [JsonPropertyName("guard_capacity")]
    public int GuardCapacity { get; init; }

    [JsonPropertyName("maintenance_capacity")]
    public int MaintenanceCapacity { get; init; }

    public SettlementSectorCapacityPreset ToPreset() => new(
        AgricultureCapacity,
        FishingCapacity,
        HuntingCapacity,
        ResourceGatheringCapacity,
        CraftingCapacity,
        TradeCapacity,
        GuardCapacity,
        MaintenanceCapacity);
}
