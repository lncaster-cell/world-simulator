using System.Text.Json;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Persistence.Saves;
using Xunit;

namespace WorldSimulator.Persistence.Tests;

public sealed class JsonWorldSaveServiceTests
{
    [Fact]
    public async Task SaveAsync_Saves_Active_Events()
    {
        var service = new JsonWorldSaveService();
        var city = CityPresets.CreateGotha();
        var clock = new SimulationClock();
        var manager = new CityEventManager();
        manager.AddEvent(new CityEvent("fire", "Fire", "Warehouse fire", 2, 5, 3));

        var filePath = Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");

        try
        {
            await service.SaveAsync(filePath, city, clock, manager);

            var json = await File.ReadAllTextAsync(filePath);
            Assert.Contains("\"Events\"", json);
            Assert.Contains("\"ActiveEvents\"", json);
            Assert.Contains("\"RemainingDays\": 3", json);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public async Task LoadAsync_Restores_Active_Events()
    {
        var loaded = await SaveThenLoadAsync(CreateManagerWithEvents(activeCount: 1, completedCount: 0));

        Assert.Single(loaded.ActiveEvents);
        Assert.Equal("fire", loaded.ActiveEvents[0].Id);
    }

    [Fact]
    public async Task LoadAsync_Restores_Completed_Events()
    {
        var loaded = await SaveThenLoadAsync(CreateManagerWithEvents(activeCount: 0, completedCount: 1));

        Assert.Single(loaded.CompletedEvents);
        Assert.Equal("storm", loaded.CompletedEvents[0].Id);
        Assert.Equal(0, loaded.CompletedEvents[0].RemainingDays);
    }

    [Fact]
    public async Task LoadAsync_Preserves_RemainingDays()
    {
        var loaded = await SaveThenLoadAsync(CreateManagerWithEvents(activeCount: 1, completedCount: 0));

        Assert.Equal(3, loaded.ActiveEvents[0].RemainingDays);
    }

    [Fact]
    public async Task SaveLoad_With_No_Events_Works()
    {
        var loaded = await SaveThenLoadAsync(new CityEventManager());

        Assert.Empty(loaded.ActiveEvents);
        Assert.Empty(loaded.CompletedEvents);
    }

    [Fact]
    public async Task LoadAsync_Old_Format_Without_Events_Returns_Empty_Lists()
    {
        var service = new JsonWorldSaveService();
        var filePath = Path.Combine(Path.GetTempPath(), $"old-world-save-{Guid.NewGuid():N}.json");

        var oldSave = new
        {
            Version = 1,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 2, Hour = 8, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            City = new { Id = "city_gotha", Name = "Гота", Population = 420, Food = 1000, Wealth = 320, Mood = 55, Security = 60, Crime = 30, Resources = 260, Goods = 140, CityState = "Stagnation" }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(oldSave, new JsonSerializerOptions { WriteIndented = true }));
            var loaded = await service.LoadAsync(filePath);

            Assert.Empty(loaded.ActiveEvents);
            Assert.Empty(loaded.CompletedEvents);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
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

    private static CityEventManager CreateManagerWithEvents(int activeCount, int completedCount)
    {
        var manager = new CityEventManager();

        if (activeCount > 0)
        {
            manager.AddEvent(new CityEvent("fire", "Fire", "Warehouse fire", 2, 5, 3));
        }

        if (completedCount > 0)
        {
            manager.Restore(manager.ActiveEvents, [new CityEvent("storm", "Storm", "Port storm", 1, 2, 0)]);
            if (activeCount == 0)
            {
                manager.Restore([], manager.CompletedEvents);
            }
        }

        return manager;
    }

    private static async Task<WorldLoadResult> SaveThenLoadAsync(CityEventManager manager)
    {
        var service = new JsonWorldSaveService();
        var city = CityPresets.CreateGotha();
        var clock = new SimulationClock();
        var filePath = Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");

        try
        {
            await service.SaveAsync(filePath, city, clock, manager);
            return await service.LoadAsync(filePath);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
