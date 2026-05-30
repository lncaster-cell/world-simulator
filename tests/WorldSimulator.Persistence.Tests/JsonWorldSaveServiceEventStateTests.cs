using WorldSimulator.Core.Time;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;
using Xunit;
using static WorldSimulator.Persistence.Tests.WorldSaveTestHelpers;

namespace WorldSimulator.Persistence.Tests;

public sealed class JsonWorldSaveServiceEventStateTests
{
    [Fact]
    public async Task LoadAsync_Does_Not_Copy_EventBucket_From_Other_City_To_Selected_City()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var sourceCityId = world.Cities[0].Id;
        var selectedCityId = world.Cities[1].Id;
        world.SelectedCityId = selectedCityId;
        var eventState = CreateEventStateWithEvents(sourceCityId);
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), eventState);

            var loaded = await service.LoadAsync(filePath);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(selectedCityId);
            Assert.Empty(selectedManager.ActiveEvents);
            Assert.Empty(selectedManager.CompletedEvents);

            var sourceManager = loaded.EventState.GetManagerOrEmpty(sourceCityId);
            Assert.Single(sourceManager.ActiveEvents);
            Assert.Equal("fire", sourceManager.ActiveEvents[0].Id);
            Assert.Single(sourceManager.CompletedEvents);
            Assert.Equal("storm", sourceManager.CompletedEvents[0].Id);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Old_Event_Format_Without_EventBuckets_Restores_Selected_City_Events()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var selectedCityId = world.Cities[1].Id;
        world.SelectedCityId = selectedCityId;
        var eventState = CreateEventStateWithEvents(selectedCityId);
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), eventState);
            var root = await ReadSavedJsonAsync(filePath);
            root["Events"]!.AsObject().Remove("EventsByCityId");
            await File.WriteAllTextAsync(filePath, root.ToJsonString());

            var loaded = await service.LoadAsync(filePath);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(selectedCityId);
            Assert.Single(selectedManager.ActiveEvents);
            Assert.Equal("fire", selectedManager.ActiveEvents[0].Id);
            Assert.Single(selectedManager.CompletedEvents);
            Assert.Equal("storm", selectedManager.CompletedEvents[0].Id);
        }
        finally { Cleanup(filePath); }
    }
}
