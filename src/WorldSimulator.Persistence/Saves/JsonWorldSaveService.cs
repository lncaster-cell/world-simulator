using System.Text.Json;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Time;

namespace WorldSimulator.Persistence.Saves;

public sealed class JsonWorldSaveService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string filePath, City city, SimulationClock clock, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(clock);

        var saveData = new WorldSaveData
        {
            SavedAtUtc = DateTime.UtcNow,
            Clock = new ClockSaveData
            {
                Day = clock.Day,
                Hour = clock.Hour,
                IsRunning = clock.IsRunning,
                AccumulatedRealTime = clock.AccumulatedRealTime,
                RealTimePerGameHour = clock.RealTimePerGameHour
            },
            City = new CitySaveData
            {
                Id = city.Id,
                Name = city.Name,
                Population = city.Population,
                Food = city.Food,
                Wealth = city.Wealth,
                Mood = city.Mood,
                Security = city.Security,
                Crime = city.Crime,
                Resources = city.Resources,
                Goods = city.Goods,
                CityState = city.CityState.ToString()
            }
        };

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, saveData, JsonOptions, cancellationToken);
    }

    public async Task<(City City, SimulationClock Clock)> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save file was not found: '{filePath}'.", filePath);
        }

        WorldSaveData? saveData;
        try
        {
            await using var stream = File.OpenRead(filePath);
            saveData = await JsonSerializer.DeserializeAsync<WorldSaveData>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Save file '{filePath}' contains invalid JSON.", ex);
        }

        if (saveData is null)
        {
            throw new InvalidDataException($"Save file '{filePath}' is empty or malformed.");
        }

        if (!Enum.TryParse<CityState>(saveData.City.CityState, ignoreCase: true, out var parsedCityState))
        {
            throw new InvalidDataException($"Save file '{filePath}' contains unknown city_state '{saveData.City.CityState}'.");
        }

        var city = new City(
            id: saveData.City.Id,
            name: saveData.City.Name,
            population: saveData.City.Population,
            food: saveData.City.Food,
            wealth: saveData.City.Wealth,
            mood: saveData.City.Mood,
            security: saveData.City.Security,
            crime: saveData.City.Crime,
            resources: saveData.City.Resources,
            goods: saveData.City.Goods,
            cityState: parsedCityState);

        var settings = new SimulationTimeSettings
        {
            RealTimePerGameHour = saveData.Clock.RealTimePerGameHour
        };

        var clock = new SimulationClock(settings);
        clock.RestoreState(
            saveData.Clock.Day,
            saveData.Clock.Hour,
            saveData.Clock.IsRunning,
            saveData.Clock.AccumulatedRealTime,
            saveData.Clock.RealTimePerGameHour);

        return (city, clock);
    }
}
