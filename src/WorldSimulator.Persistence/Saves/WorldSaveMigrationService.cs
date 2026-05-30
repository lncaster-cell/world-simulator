namespace WorldSimulator.Persistence.Saves;

internal sealed class WorldSaveMigrationService
{
    public const int CurrentSaveVersion = 4;

    private static readonly IReadOnlyList<IWorldSaveMigrationStep> DefaultMigrationSteps =
    [
        new LegacyCityToWorldSaveMigrationStep(),
        new RestoreWorldCollectionsSaveMigrationStep(),
        new RestoreCityStateSaveMigrationStep(),
        new RestoreSectorCapacityProfilesSaveMigrationStep(),
        new RestoreRouteFieldsSaveMigrationStep(),
        new RestoreSelectedWorldItemsSaveMigrationStep()
    ];

    private readonly IReadOnlyList<IWorldSaveMigrationStep> _migrationSteps;

    internal WorldSaveMigrationService()
        : this(DefaultMigrationSteps)
    {
    }

    internal WorldSaveMigrationService(IEnumerable<IWorldSaveMigrationStep> migrationSteps)
    {
        ArgumentNullException.ThrowIfNull(migrationSteps);
        _migrationSteps = migrationSteps
            .Select((step, order) => new { Step = step, Order = order })
            .OrderBy(item => item.Step.FromVersion)
            .ThenBy(item => item.Step.ToVersion)
            .ThenBy(item => item.Order)
            .Select(item => item.Step)
            .ToArray();
    }

    public WorldSaveData Migrate(WorldSaveData saveData, string filePath)
    {
        ArgumentNullException.ThrowIfNull(saveData);

        if (saveData.Version <= 0 || saveData.Version > CurrentSaveVersion)
            throw new InvalidDataException($"Save file '{filePath}' has unsupported version '{saveData.Version}'. Expected '{CurrentSaveVersion}' or earlier.");

        saveData.Clock ??= new ClockSaveData();
        saveData.Events ??= new EventSaveData();

        foreach (var migrationStep in GetApplicableMigrationSteps(saveData.Version))
        {
            migrationStep.Apply(saveData, filePath);
        }

        if (saveData.World is null)
            throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");

        saveData.Version = CurrentSaveVersion;
        return saveData;
    }

    private IEnumerable<IWorldSaveMigrationStep> GetApplicableMigrationSteps(int saveVersion)
    {
        return _migrationSteps.Where(step => step.FromVersion <= saveVersion && saveVersion < step.ToVersion);
    }
}
