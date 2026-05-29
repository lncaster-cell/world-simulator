using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldSimulator.Core.World;

internal static class RiviaSettlementPresetDataLoader
{
    private const string RiviaSettlementsResourceName = "WorldSimulator.Core.Data.settlements.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<RiviaSettlementPreset> LoadPresets()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RiviaSettlementsResourceName);
            if (stream is not null)
            {
                return LoadPresets(stream);
            }
        }
        catch (JsonException)
        {
            // Keep the world bootable while settlement data is being externalized.
        }
        catch (InvalidDataException)
        {
            // Keep the world bootable while settlement data is being externalized.
        }

        return RiviaSettlementHardcodedPresets.Create();
    }

    private static IReadOnlyList<RiviaSettlementPreset> LoadPresets(Stream stream)
    {
        var dataFile = JsonSerializer.Deserialize<RiviaSettlementPresetDataFile>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Settlement data '{RiviaSettlementsResourceName}' is empty.");

        if (!string.Equals(dataFile.SchemaVersion, "rivia_settlements_v1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported Rivia settlement schema '{dataFile.SchemaVersion}'.");
        }

        if (dataFile.Settlements.Count == 0)
        {
            throw new InvalidDataException($"Settlement data '{RiviaSettlementsResourceName}' contains no settlements.");
        }

        return dataFile.Settlements.Select(x => x.ToPreset()).ToList();
    }

    private sealed record RiviaSettlementPresetDataFile
    {
        [JsonPropertyName("schema_version")]
        public required string SchemaVersion { get; init; }

        [JsonPropertyName("region_id")]
        public required string RegionId { get; init; }

        [JsonPropertyName("settlements")]
        public required List<RiviaSettlementPresetData> Settlements { get; init; }
    }
}
