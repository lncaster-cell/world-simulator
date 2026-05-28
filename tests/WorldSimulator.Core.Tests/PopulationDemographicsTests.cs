using WorldSimulator.Core.Cities;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class PopulationDemographicsTests
{
    [Fact]
    public void CreateDefaultHuman_PreservesTotalPopulation()
    {
        var demographics = CityPopulationDemographics.CreateDefaultHuman(420);

        Assert.Equal(420, demographics.TotalPopulation);
        Assert.Single(demographics.RaceGroups);
        Assert.Equal("human", demographics.RaceGroups[0].RaceId);
        Assert.True(demographics.Children > 0);
        Assert.True(demographics.AdultMen > 0);
        Assert.True(demographics.AdultWomen > 0);
        Assert.True(demographics.Elderly > 0);
    }

    [Fact]
    public void RacePopulationGroup_ClampsNegativeValues()
    {
        var group = new RacePopulationGroup
        {
            RaceId = "human",
            Children = -1,
            AdultMen = -2,
            AdultWomen = -3,
            Elderly = -4
        };

        Assert.Equal(0, group.Children);
        Assert.Equal(0, group.AdultMen);
        Assert.Equal(0, group.AdultWomen);
        Assert.Equal(0, group.Elderly);
        Assert.Equal(0, group.TotalPopulation);
    }

    [Fact]
    public void City_DefaultsDemographicsFromPopulation()
    {
        var city = new City(
            "gotha",
            "Гота",
            420,
            1000m,
            320m,
            55,
            60,
            30,
            260m,
            140m,
            CityState.Stagnation);

        Assert.Equal(city.Population, city.Demographics.TotalPopulation);
    }
}
