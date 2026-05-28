using WorldSimulator.Core.Cities;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityInfrastructureTests
{
    [Fact]
    public void Defaults_AllLevelsToOne()
    {
        var infrastructure = new CityInfrastructure();

        Assert.Equal(1, infrastructure.HousingLevel);
        Assert.Equal(1, infrastructure.UrbanLevel);
        Assert.Equal(1, infrastructure.ProductionLevel);
        Assert.Equal(1, infrastructure.MilitaryLevel);
    }

    [Fact]
    public void Levels_AreClampedBetweenOneAndFive()
    {
        var infrastructure = new CityInfrastructure
        {
            HousingLevel = 0,
            UrbanLevel = 6,
            ProductionLevel = -10,
            MilitaryLevel = 99
        };

        Assert.Equal(1, infrastructure.HousingLevel);
        Assert.Equal(5, infrastructure.UrbanLevel);
        Assert.Equal(1, infrastructure.ProductionLevel);
        Assert.Equal(5, infrastructure.MilitaryLevel);
    }

    [Fact]
    public void City_CanCarryInfrastructureState()
    {
        var city = new City(
            "gotha",
            "Гота",
            420,
            1000m,
            320m,
            50,
            60,
            30,
            260m,
            140m,
            CityState.Stable,
            new CityInfrastructure
            {
                HousingLevel = 2,
                UrbanLevel = 3,
                ProductionLevel = 4,
                MilitaryLevel = 5
            });

        Assert.Equal(2, city.Infrastructure.HousingLevel);
        Assert.Equal(3, city.Infrastructure.UrbanLevel);
        Assert.Equal(4, city.Infrastructure.ProductionLevel);
        Assert.Equal(5, city.Infrastructure.MilitaryLevel);
    }
}
