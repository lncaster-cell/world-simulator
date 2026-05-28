namespace WorldSimulator.Core.Cities;

public sealed class CityInfrastructure
{
    public const int MinLevel = 1;
    public const int MaxLevel = 5;

    private int _housingLevel = MinLevel;
    private int _urbanLevel = MinLevel;
    private int _productionLevel = MinLevel;
    private int _militaryLevel = MinLevel;

    public int HousingLevel
    {
        get => _housingLevel;
        set => _housingLevel = ClampLevel(value);
    }

    public int UrbanLevel
    {
        get => _urbanLevel;
        set => _urbanLevel = ClampLevel(value);
    }

    public int ProductionLevel
    {
        get => _productionLevel;
        set => _productionLevel = ClampLevel(value);
    }

    public int MilitaryLevel
    {
        get => _militaryLevel;
        set => _militaryLevel = ClampLevel(value);
    }

    public static int ClampLevel(int level) => Math.Clamp(level, MinLevel, MaxLevel);
}
