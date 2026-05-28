using WorldSimulator.Core.Cities;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityPopulationDemographicsSynchronizerTests
{
    [Fact]
    public void SynchronizeToPopulation_Decrease_PreservesExactTotal()
    {
        var demographics = CityPopulationDemographics.CreateDefaultHuman(420);

        new CityPopulationDemographicsSynchronizer().SynchronizeToPopulation(demographics, 390);

        Assert.Equal(390, demographics.TotalPopulation);
        Assert.Single(demographics.RaceGroups);
        Assert.Equal("human", demographics.RaceGroups[0].RaceId);
    }

    [Fact]
    public void SynchronizeToPopulation_Increase_PreservesExactTotal()
    {
        var demographics = CityPopulationDemographics.CreateDefaultHuman(420);

        new CityPopulationDemographicsSynchronizer().SynchronizeToPopulation(demographics, 460);

        Assert.Equal(460, demographics.TotalPopulation);
        Assert.True(demographics.AdultMen > 0);
        Assert.True(demographics.AdultWomen > 0);
    }

    [Fact]
    public void SynchronizeToPopulation_ZeroPopulation_ClearsGroupsButKeepsRaceIds()
    {
        var demographics = CityPopulationDemographics.CreateDefaultHuman(420);

        new CityPopulationDemographicsSynchronizer().SynchronizeToPopulation(demographics, 0);

        Assert.Equal(0, demographics.TotalPopulation);
        Assert.Single(demographics.RaceGroups);
        Assert.Equal("human", demographics.RaceGroups[0].RaceId);
    }

    [Fact]
    public void SynchronizeToPopulation_EmptyDemographics_CreatesDefaultHumanPopulation()
    {
        var demographics = new CityPopulationDemographics();

        new CityPopulationDemographicsSynchronizer().SynchronizeToPopulation(demographics, 100);

        Assert.Equal(100, demographics.TotalPopulation);
        Assert.Single(demographics.RaceGroups);
        Assert.Equal("human", demographics.RaceGroups[0].RaceId);
    }

    [Fact]
    public void SynchronizeToPopulation_MultipleRaceGroups_PreservesRaceGroupsAndExactTotal()
    {
        var demographics = new CityPopulationDemographics();
        demographics.RaceGroups.Add(new RacePopulationGroup
        {
            RaceId = "human",
            Children = 20,
            AdultMen = 40,
            AdultWomen = 40,
            Elderly = 10
        });
        demographics.RaceGroups.Add(new RacePopulationGroup
        {
            RaceId = "dwarf",
            Children = 5,
            AdultMen = 15,
            AdultWomen = 15,
            Elderly = 5
        });

        new CityPopulationDemographicsSynchronizer().SynchronizeToPopulation(demographics, 200);

        Assert.Equal(200, demographics.TotalPopulation);
        Assert.Contains(demographics.RaceGroups, group => group.RaceId == "human");
        Assert.Contains(demographics.RaceGroups, group => group.RaceId == "dwarf");
    }
}
