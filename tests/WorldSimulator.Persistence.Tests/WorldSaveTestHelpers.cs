using System.Text.Json.Nodes;
using WorldSimulator.Core.Events;

namespace WorldSimulator.Persistence.Tests;

internal static class WorldSaveTestHelpers
{
    public static WorldEventState CreateEventStateWithEvents(string cityId)
    {
        var manager = new CityEventManager();
        manager.Restore([new CityEvent("fire", "Fire", "Warehouse fire", 2, 5, 3)], [new CityEvent("storm", "Storm", "Port storm", 1, 2, 0)]);
        var state = new WorldEventState();
        state.SetManager(cityId, manager);
        return state;
    }

    public static async Task<JsonObject> ReadSavedJsonAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonNode.Parse(json)!.AsObject();
    }

    public static string TempFile() => Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");

    public static void Cleanup(string p)
    {
        if (File.Exists(p)) File.Delete(p);
    }
}
