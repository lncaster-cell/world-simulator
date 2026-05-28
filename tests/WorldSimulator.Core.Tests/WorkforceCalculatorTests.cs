using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class WorkforceCalculatorTests
{
    [Fact]
    public void Calculate_UsesDemographicsAndLawProfile()
    {
        var demographics = new CityPopulationDemographics();
        demographics.RaceGroups.Add(new RacePopulationGroup
        {
            RaceId = "human",
            Children = 100,
            AdultMen = 200,
            AdultWomen = 220,
            Elderly = 60
        });
        var lawProfile = new WorkforceLawProfile
        {
            AdultMaleWorkRate = 0.90m,
            AdultFemaleWorkRate = 0.50m,
            ElderlyWorkRate = 0.10m,
            ChildLaborRate = 0.00m,
            GlobalWorkforceModifier = 1.00m
        };

        var result = new WorkforceCalculator().Calculate(demographics, lawProfile);

        Assert.Equal(180m, result.AdultMaleWorkers);
        Assert.Equal(110m, result.AdultFemaleWorkers);
        Assert.Equal(6m, result.ElderlyWorkers);
        Assert.Equal(0m, result.ChildWorkers);
        Assert.Equal(296, result.TotalWorkers);
    }

    [Fact]
    public void Calculate_AppliesGlobalModifier()
    {
        var demographics = new CityPopulationDemographics();
        demographics.RaceGroups.Add(new RacePopulationGroup
        {
            RaceId = "human",
            Children = 0,
            AdultMen = 100,
            AdultWomen = 0,
            Elderly = 0
        });
        var lawProfile = new WorkforceLawProfile
        {
            AdultMaleWorkRate = 1.00m,
            GlobalWorkforceModifier = 0.50m
        };

        var result = new WorkforceCalculator().Calculate(demographics, lawProfile);

        Assert.Equal(50, result.TotalWorkers);
    }

    [Fact]
    public void LawProfile_ClampsRates()
    {
        var lawProfile = new WorkforceLawProfile
        {
            AdultMaleWorkRate = 2.00m,
            AdultFemaleWorkRate = -1.00m,
            ElderlyWorkRate = 3.00m,
            ChildLaborRate = -5.00m,
            GlobalWorkforceModifier = 9.00m
        };

        Assert.Equal(1.00m, lawProfile.AdultMaleWorkRate);
        Assert.Equal(0.00m, lawProfile.AdultFemaleWorkRate);
        Assert.Equal(1.00m, lawProfile.ElderlyWorkRate);
        Assert.Equal(0.00m, lawProfile.ChildLaborRate);
        Assert.Equal(1.50m, lawProfile.GlobalWorkforceModifier);
    }
}
