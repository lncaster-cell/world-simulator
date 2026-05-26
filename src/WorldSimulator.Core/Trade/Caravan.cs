namespace WorldSimulator.Core.Trade;

public sealed class Caravan
{
    public required string Id { get; init; }
    public required string OwnerSettlementId { get; init; }
    public required CaravanType Type { get; init; }
    public required decimal Capacity { get; init; }
    public required int RequiredWorkers { get; init; }
    public required bool IsAvailable { get; init; }
}
