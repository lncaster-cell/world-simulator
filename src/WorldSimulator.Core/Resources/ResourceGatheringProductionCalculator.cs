using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class ResourceGatheringProductionCalculator
{
    private const decimal NaturalPotential = 14m;
    private const int RequiredWorkers = 25;
    private const decimal OverstaffBonusCap = 0.08m;

    public ResourceGatheringProductionResult Calculate(City city)
    {
        ArgumentNullException.ThrowIfNull(city);
        var securityModifier = ProductionBalancePolicy.GetSecurityModifier(city.Security, ProductionDomain.Resource);
        var stateModifier = ProductionBalancePolicy.GetStateModifier(city.CityState, ProductionDomain.Resource);

        if (city.Population <= 0 || city.CityState == CityState.Abandoned)
        {
            return new ResourceGatheringProductionResult { NaturalPotential = NaturalPotential, RequiredWorkers = RequiredWorkers, AssignedWorkers = 0, WorkerCoverage = 0m, ExtraWorkers = 0, OverstaffBonus = 0m, SecurityModifier = securityModifier, StateModifier = stateModifier, FinalOutput = 0m };
        }

        var assignedWorkers = Math.Max(0, city.Population / 15);
        var workerCoverage = RequiredWorkers > 0 ? Math.Min((decimal)assignedWorkers / RequiredWorkers, 1m) : 0m;
        var extraWorkers = Math.Max(assignedWorkers - RequiredWorkers, 0);
        var overstaffBonus = 0m;

        if (extraWorkers > 0 && RequiredWorkers > 0)
        {
            var overstaffRatio = (decimal)extraWorkers / RequiredWorkers;
            overstaffBonus = NaturalPotential * OverstaffBonusCap * (1m - (1m / (1m + overstaffRatio)));
        }

        var finalOutput = decimal.Round(Math.Max(0m, (NaturalPotential * workerCoverage + overstaffBonus) * securityModifier * stateModifier), 2);

        return new ResourceGatheringProductionResult { NaturalPotential = NaturalPotential, RequiredWorkers = RequiredWorkers, AssignedWorkers = assignedWorkers, WorkerCoverage = workerCoverage, ExtraWorkers = extraWorkers, OverstaffBonus = overstaffBonus, SecurityModifier = securityModifier, StateModifier = stateModifier, FinalOutput = finalOutput };
    }
}
