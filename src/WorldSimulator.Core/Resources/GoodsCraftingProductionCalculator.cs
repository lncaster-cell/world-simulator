using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class GoodsCraftingProductionCalculator
{
    private const decimal NaturalPotential = 10m;
    private const int RequiredWorkers = 30;
    private const decimal OverstaffBonusCap = 0.05m;
    private const decimal ResourceCostPerGoods = 1.20m;

    public GoodsCraftingProductionResult Calculate(City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        var moodModifier = GetMoodModifier(city.Mood);
        var securityModifier = GetSecurityModifier(city.Security);
        var stateModifier = GetStateModifier(city.CityState);
        var resourcesAvailable = Math.Max(0m, city.Resources);

        if (city.Population <= 0 || city.CityState == CityState.Abandoned)
        {
            return new GoodsCraftingProductionResult
            {
                NaturalPotential = NaturalPotential,
                RequiredWorkers = RequiredWorkers,
                AssignedWorkers = 0,
                WorkerCoverage = 0m,
                ExtraWorkers = 0,
                OverstaffBonus = 0m,
                MoodModifier = moodModifier,
                SecurityModifier = securityModifier,
                StateModifier = stateModifier,
                ResourceCostPerGoods = ResourceCostPerGoods,
                PotentialGoodsOutput = 0m,
                ResourcesNeeded = 0m,
                ResourcesAvailable = resourcesAvailable,
                ResourcesConsumed = 0m,
                GoodsProduced = 0m
            };
        }

        var assignedWorkers = Math.Max(0, city.Population / 18);
        var workerCoverage = RequiredWorkers > 0
            ? Math.Min((decimal)assignedWorkers / RequiredWorkers, 1m)
            : 0m;

        var extraWorkers = Math.Max(assignedWorkers - RequiredWorkers, 0);
        var overstaffBonus = WorkforceBonusCalculator.CalculateOverstaffBonus(
            NaturalPotential,
            OverstaffBonusCap,
            extraWorkers,
            RequiredWorkers);

        var potentialGoodsOutput = Math.Max(0m,
            (NaturalPotential * workerCoverage + overstaffBonus) * moodModifier * securityModifier * stateModifier);

        var resourcesNeeded = Math.Max(0m, potentialGoodsOutput * ResourceCostPerGoods);
        var goodsProduced = 0m;
        var resourcesConsumed = 0m;

        if (resourcesAvailable > 0m)
        {
            if (resourcesAvailable >= resourcesNeeded)
            {
                goodsProduced = potentialGoodsOutput;
                resourcesConsumed = resourcesNeeded;
            }
            else
            {
                goodsProduced = resourcesAvailable / ResourceCostPerGoods;
                resourcesConsumed = resourcesAvailable;
            }
        }

        goodsProduced = decimal.Round(Math.Max(0m, goodsProduced), 2);
        resourcesConsumed = decimal.Round(Math.Max(0m, resourcesConsumed), 2);

        return new GoodsCraftingProductionResult
        {
            NaturalPotential = NaturalPotential,
            RequiredWorkers = RequiredWorkers,
            AssignedWorkers = assignedWorkers,
            WorkerCoverage = workerCoverage,
            ExtraWorkers = extraWorkers,
            OverstaffBonus = overstaffBonus,
            MoodModifier = moodModifier,
            SecurityModifier = securityModifier,
            StateModifier = stateModifier,
            ResourceCostPerGoods = ResourceCostPerGoods,
            PotentialGoodsOutput = potentialGoodsOutput,
            ResourcesNeeded = resourcesNeeded,
            ResourcesAvailable = resourcesAvailable,
            ResourcesConsumed = resourcesConsumed,
            GoodsProduced = goodsProduced
        };
    }

    private static decimal GetMoodModifier(int mood)
    {
        return mood switch
        {
            >= 70 => 1.05m,
            >= 40 => 1.00m,
            >= 20 => 0.80m,
            _ => 0.55m
        };
    }

    private static decimal GetSecurityModifier(int security)
    {
        return security switch
        {
            >= 70 => 1.05m,
            >= 40 => 1.00m,
            >= 20 => 0.80m,
            _ => 0.55m
        };
    }

    private static decimal GetStateModifier(CityState state)
    {
        return state switch
        {
            CityState.Abandoned => 0.00m,
            CityState.Collapse => 0.15m,
            CityState.Famine => 0.45m,
            CityState.FoodShortage => 0.65m,
            CityState.Unrest => 0.45m,
            CityState.CrimeProblem => 0.70m,
            CityState.EconomicDecline => 0.75m,
            CityState.Stagnation => 0.85m,
            CityState.Stable => 1.00m,
            CityState.Prosperous => 1.10m,
            CityState.Recovery => 0.90m,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
        };
    }
}
