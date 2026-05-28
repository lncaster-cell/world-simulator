using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Workforce;

public sealed class WorkforceCalculator
{
    public WorkforceCalculationResult Calculate(CityPopulationDemographics demographics, WorkforceLawProfile lawProfile)
    {
        ArgumentNullException.ThrowIfNull(demographics);
        ArgumentNullException.ThrowIfNull(lawProfile);

        var adultMaleWorkers = demographics.AdultMen * lawProfile.AdultMaleWorkRate;
        var adultFemaleWorkers = demographics.AdultWomen * lawProfile.AdultFemaleWorkRate;
        var elderlyWorkers = demographics.Elderly * lawProfile.ElderlyWorkRate;
        var childWorkers = demographics.Children * lawProfile.ChildLaborRate;
        var potentialWorkers = adultMaleWorkers + adultFemaleWorkers + elderlyWorkers + childWorkers;
        var totalWorkers = (int)decimal.Round(
            Math.Max(0m, potentialWorkers * lawProfile.GlobalWorkforceModifier),
            0,
            MidpointRounding.AwayFromZero);

        return new WorkforceCalculationResult(
            demographics.Children,
            demographics.AdultMen,
            demographics.AdultWomen,
            demographics.Elderly,
            adultMaleWorkers,
            adultFemaleWorkers,
            elderlyWorkers,
            childWorkers,
            potentialWorkers,
            lawProfile.GlobalWorkforceModifier,
            totalWorkers);
    }
}
