namespace WorldSimulator.Core.Workforce;

public sealed class WorkforceLawProfile
{
    private decimal _adultMaleWorkRate = 0.90m;
    private decimal _adultFemaleWorkRate = 0.65m;
    private decimal _elderlyWorkRate = 0.10m;
    private decimal _childLaborRate = 0.00m;
    private decimal _globalWorkforceModifier = 1.00m;

    public decimal AdultMaleWorkRate
    {
        get => _adultMaleWorkRate;
        set => _adultMaleWorkRate = ClampRate(value);
    }

    public decimal AdultFemaleWorkRate
    {
        get => _adultFemaleWorkRate;
        set => _adultFemaleWorkRate = ClampRate(value);
    }

    public decimal ElderlyWorkRate
    {
        get => _elderlyWorkRate;
        set => _elderlyWorkRate = ClampRate(value);
    }

    public decimal ChildLaborRate
    {
        get => _childLaborRate;
        set => _childLaborRate = ClampRate(value);
    }

    public decimal GlobalWorkforceModifier
    {
        get => _globalWorkforceModifier;
        set => _globalWorkforceModifier = Math.Clamp(value, 0.20m, 1.50m);
    }

    private static decimal ClampRate(decimal value) => Math.Clamp(value, 0m, 1m);
}
