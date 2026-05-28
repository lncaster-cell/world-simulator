namespace WorldSimulator.Core.Cities;

public sealed class CityPopulationDemographicsSynchronizer
{
    public void SynchronizeToPopulation(CityPopulationDemographics demographics, int targetPopulation)
    {
        ArgumentNullException.ThrowIfNull(demographics);

        var safeTargetPopulation = Math.Max(0, targetPopulation);
        if (demographics.TotalPopulation == safeTargetPopulation)
        {
            return;
        }

        if (safeTargetPopulation == 0)
        {
            demographics.ReplaceWith(demographics.RaceGroups.Select(group => new RacePopulationGroup
            {
                RaceId = group.RaceId,
                Children = 0,
                AdultMen = 0,
                AdultWomen = 0,
                Elderly = 0
            }));
            return;
        }

        if (demographics.TotalPopulation <= 0 || demographics.RaceGroups.Count == 0)
        {
            demographics.ReplaceWith(CityPopulationDemographics.CreateDefaultHuman(safeTargetPopulation).RaceGroups);
            return;
        }

        var buckets = BuildBuckets(demographics, safeTargetPopulation);
        var assignedPopulation = buckets.Sum(bucket => bucket.Value);
        var remainingPopulation = safeTargetPopulation - assignedPopulation;

        foreach (var bucket in buckets
            .OrderByDescending(bucket => bucket.FractionalRemainder)
            .ThenByDescending(bucket => bucket.OriginalValue)
            .Take(remainingPopulation))
        {
            bucket.Value++;
        }

        demographics.ReplaceWith(BuildRaceGroups(demographics, buckets));
    }

    private static List<PopulationBucket> BuildBuckets(CityPopulationDemographics demographics, int targetPopulation)
    {
        var currentTotal = demographics.TotalPopulation;
        var scale = (decimal)targetPopulation / currentTotal;
        var buckets = new List<PopulationBucket>();

        foreach (var group in demographics.RaceGroups)
        {
            buckets.Add(CreateBucket(group.RaceId, PopulationBucketKind.Children, group.Children, scale));
            buckets.Add(CreateBucket(group.RaceId, PopulationBucketKind.AdultMen, group.AdultMen, scale));
            buckets.Add(CreateBucket(group.RaceId, PopulationBucketKind.AdultWomen, group.AdultWomen, scale));
            buckets.Add(CreateBucket(group.RaceId, PopulationBucketKind.Elderly, group.Elderly, scale));
        }

        return buckets;
    }

    private static PopulationBucket CreateBucket(string raceId, PopulationBucketKind kind, int originalValue, decimal scale)
    {
        var rawValue = originalValue * scale;
        var value = (int)decimal.Floor(rawValue);
        return new PopulationBucket(raceId, kind, originalValue, value, rawValue - value);
    }

    private static IEnumerable<RacePopulationGroup> BuildRaceGroups(CityPopulationDemographics demographics, IReadOnlyCollection<PopulationBucket> buckets)
    {
        foreach (var sourceGroup in demographics.RaceGroups)
        {
            yield return new RacePopulationGroup
            {
                RaceId = sourceGroup.RaceId,
                Children = GetBucketValue(buckets, sourceGroup.RaceId, PopulationBucketKind.Children),
                AdultMen = GetBucketValue(buckets, sourceGroup.RaceId, PopulationBucketKind.AdultMen),
                AdultWomen = GetBucketValue(buckets, sourceGroup.RaceId, PopulationBucketKind.AdultWomen),
                Elderly = GetBucketValue(buckets, sourceGroup.RaceId, PopulationBucketKind.Elderly)
            };
        }
    }

    private static int GetBucketValue(IEnumerable<PopulationBucket> buckets, string raceId, PopulationBucketKind kind)
    {
        return buckets.First(bucket => bucket.RaceId == raceId && bucket.Kind == kind).Value;
    }

    private enum PopulationBucketKind
    {
        Children,
        AdultMen,
        AdultWomen,
        Elderly
    }

    private sealed class PopulationBucket
    {
        public PopulationBucket(string raceId, PopulationBucketKind kind, int originalValue, int value, decimal fractionalRemainder)
        {
            RaceId = raceId;
            Kind = kind;
            OriginalValue = originalValue;
            Value = value;
            FractionalRemainder = fractionalRemainder;
        }

        public string RaceId { get; }
        public PopulationBucketKind Kind { get; }
        public int OriginalValue { get; }
        public int Value { get; set; }
        public decimal FractionalRemainder { get; }
    }
}
