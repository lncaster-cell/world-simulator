namespace WorldSimulator.Persistence.Saves;

internal interface IWorldSaveMigrationStep
{
    int FromVersion { get; }
    int ToVersion { get; }
    void Apply(WorldSaveData saveData, string filePath);
}
