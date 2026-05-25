using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Time;
using WorldSimulator.Persistence.Saves;
using Xunit;

namespace WorldSimulator.Persistence.Tests;

public sealed class JsonWorldSaveServiceTests
{
    [Fact]
    public async Task SaveAsync_Creates_Json_File_For_Gotha_And_Clock()
    {
        var service = new JsonWorldSaveService();
        var city = CityPresets.CreateGotha();
        var clock = new SimulationClock();
        clock.Start();
        clock.Advance(TimeSpan.FromMinutes(11));

        var filePath = Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");

        try
        {
            await service.SaveAsync(filePath, city, clock);

            Assert.True(File.Exists(filePath));
            var json = await File.ReadAllTextAsync(filePath);
            Assert.Contains("\n  \"Clock\":", json);
            Assert.Contains("\n  \"City\":", json);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_Restores_City_Values_Exactly()
    {
        var service = new JsonWorldSaveService();
        var city = CityPresets.CreateGotha();
        city.Population = 555;
        city.Food = 777m;
        city.Wealth = 12m;
        city.Mood = 42;
        city.Security = 43;
        city.Crime = 44;
        city.Resources = 45m;
        city.Goods = 46m;

        var clock = new SimulationClock(new SimulationTimeSettings { RealTimePerGameHour = TimeSpan.FromMinutes(2) });
        clock.RestoreState(3, 7, true, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(2));

        var filePath = Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");

        try
        {
            await service.SaveAsync(filePath, city, clock);
            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(city.Id, loaded.City.Id);
            Assert.Equal(city.Name, loaded.City.Name);
            Assert.Equal(city.Population, loaded.City.Population);
            Assert.Equal(city.Food, loaded.City.Food);
            Assert.Equal(city.Wealth, loaded.City.Wealth);
            Assert.Equal(city.Mood, loaded.City.Mood);
            Assert.Equal(city.Security, loaded.City.Security);
            Assert.Equal(city.Crime, loaded.City.Crime);
            Assert.Equal(city.Resources, loaded.City.Resources);
            Assert.Equal(city.Goods, loaded.City.Goods);
            Assert.Equal(city.CityState, loaded.City.CityState);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_Restores_Clock_Values_Exactly()
    {
        var service = new JsonWorldSaveService();
        var city = CityPresets.CreateGotha();
        var clock = new SimulationClock(new SimulationTimeSettings { RealTimePerGameHour = TimeSpan.FromSeconds(30) });
        clock.RestoreState(8, 23, true, TimeSpan.FromSeconds(29), TimeSpan.FromSeconds(30));

        var filePath = Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");

        try
        {
            await service.SaveAsync(filePath, city, clock);
            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(8, loaded.Clock.Day);
            Assert.Equal(23, loaded.Clock.Hour);
            Assert.True(loaded.Clock.IsRunning);
            Assert.Equal(TimeSpan.FromSeconds(29), loaded.Clock.AccumulatedRealTime);
            Assert.Equal(TimeSpan.FromSeconds(30), loaded.Clock.RealTimePerGameHour);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_Missing_File_Throws_Clear_Exception()
    {
        var service = new JsonWorldSaveService();
        var filePath = Path.Combine(Path.GetTempPath(), $"missing-save-{Guid.NewGuid():N}.json");

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => service.LoadAsync(filePath));

        Assert.Contains("Save file was not found", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_Invalid_Clock_Data_Is_Rejected()
    {
        var service = new JsonWorldSaveService();
        var filePath = Path.Combine(Path.GetTempPath(), $"invalid-save-{Guid.NewGuid():N}.json");

        const string invalidJson = """
        {
          "Version": 1,
          "SavedAtUtc": "2026-01-01T00:00:00Z",
          "Clock": {
            "Day": 0,
            "Hour": 9,
            "IsRunning": false,
            "AccumulatedRealTime": "00:00:00",
            "RealTimePerGameHour": "00:05:00"
          },
          "City": {
            "Id": "city_gotha",
            "Name": "Гота",
            "Population": 420,
            "Food": 1000,
            "Wealth": 320,
            "Mood": 55,
            "Security": 60,
            "Crime": 30,
            "Resources": 260,
            "Goods": 140,
            "CityState": "Stagnation"
          }
        }
        """;

        try
        {
            await File.WriteAllTextAsync(filePath, invalidJson);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.LoadAsync(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
