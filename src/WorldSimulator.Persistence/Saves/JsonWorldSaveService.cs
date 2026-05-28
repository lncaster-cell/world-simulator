using System.Text.Json;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

public sealed class JsonWorldSaveService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly WorldSaveMigrationService _migrationService = new();

    public async Task SaveAsync(string filePath, SimulationWorld world, SimulationClock clock, WorldEventState eventState, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(eventState);

        var saveData = new WorldSaveData
        {
            Version = WorldSaveMigrationService.CurrentSaveVersion,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new ClockSaveData
            {
                Day = clock.Day,
                Hour = clock.Hour,
                IsRunning = clock.IsRunning,
                AccumulatedRealTime = clock.AccumulatedRealTime,
                RealTimePerGameHour = clock.RealTimePerGameHour
            },
            World = WorldSaveMapper.ToSaveData(world),
            Events = WorldSaveMapper.ToSaveData(eventState, world.SelectedCityId)
        };

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, saveData, JsonOptions, cancellationToken);
    }

    public async Task<WorldLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Save file was not found: '{filePath}'.", filePath);

        var saveData = await ReadSaveDataAsync(filePath, cancellationToken);
        saveData = _migrationService.Migrate(saveData, filePath);

        var world = WorldSaveMapper.ToCoreWorld(saveData.World!, filePath);
        WorldSaveValidator.ValidateWorld(world, filePath);

        var settings = new SimulationTimeSettings { RealTimePerGameHour = saveData.Clock.RealTimePerGameHour };
        var clock = new SimulationClock(settings);
        clock.RestoreState(saveData.Clock.Day, saveData.Clock.Hour, saveData.Clock.IsRunning, saveData.Clock.AccumulatedRealTime, saveData.Clock.RealTimePerGameHour);

        var eventState = WorldSaveMapper.ToCoreEventState(saveData.Events, world.SelectedCityId);

        return new WorldLoadResult(world, clock, eventState);
    }

    private static async Task<WorldSaveData> ReadSaveDataAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            if (stream.Length == 0) throw new InvalidDataException($"Save file '{filePath}' is empty.");
            var saveData = await JsonSerializer.DeserializeAsync<WorldSaveData>(stream, JsonOptions, cancellationToken);
            return saveData ?? throw new InvalidDataException($"Save file '{filePath}' is empty or malformed.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Save file '{filePath}' contains invalid JSON.", ex);
        }
    }
}
