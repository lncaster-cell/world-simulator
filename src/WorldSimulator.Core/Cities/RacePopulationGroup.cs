namespace WorldSimulator.Core.Cities;

public sealed class RacePopulationGroup
{
    private string _raceId = string.Empty;
    private int _children;
    private int _adultMen;
    private int _adultWomen;
    private int _elderly;

    public string RaceId
    {
        get => _raceId;
        set => _raceId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Race id must not be empty.", nameof(value))
            : value;
    }

    public int Children
    {
        get => _children;
        set => _children = Math.Max(0, value);
    }

    public int AdultMen
    {
        get => _adultMen;
        set => _adultMen = Math.Max(0, value);
    }

    public int AdultWomen
    {
        get => _adultWomen;
        set => _adultWomen = Math.Max(0, value);
    }

    public int Elderly
    {
        get => _elderly;
        set => _elderly = Math.Max(0, value);
    }

    public int TotalPopulation => Children + AdultMen + AdultWomen + Elderly;
}
