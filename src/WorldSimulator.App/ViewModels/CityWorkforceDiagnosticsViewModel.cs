using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class CityWorkforceDiagnosticsViewModel
{
    public CityWorkforceDiagnosticsViewModel(SimulationWorld? world, City? city)
    {
        World = world;
        City = city;
        LawProfile = new WorkforceLawProfile();

        if (world is null || city is null)
        {
            Rows = [CityWorkforceDiagnosticsRowViewModel.Message("Данные города недоступны.")];
            return;
        }

        CapacityProfile = world.FindSettlementSectorCapacityProfile(city.Id);
        if (CapacityProfile is null)
        {
            Rows = [CityWorkforceDiagnosticsRowViewModel.Message("Для выбранного города не найден профиль локальных лимитов секторов.")];
            return;
        }

        Allocation = new CityWorkforceAllocator().Allocate(city, CapacityProfile, LawProfile);
        Rows = BuildRows(city, CapacityProfile, LawProfile, Allocation);
    }

    public SimulationWorld? World { get; }

    public City? City { get; }

    public SettlementSectorCapacityProfile? CapacityProfile { get; }

    public WorkforceLawProfile LawProfile { get; }

    public CityWorkforceAllocation? Allocation { get; }

    public IReadOnlyList<CityWorkforceDiagnosticsRowViewModel> Rows { get; }

    private static IReadOnlyList<CityWorkforceDiagnosticsRowViewModel> BuildRows(
        City city,
        SettlementSectorCapacityProfile capacity,
        WorkforceLawProfile lawProfile,
        CityWorkforceAllocation allocation)
    {
        var rows = new List<CityWorkforceDiagnosticsRowViewModel>
        {
            CityWorkforceDiagnosticsRowViewModel.SectionHeader("Население"),
            Row("Население", "Всего жителей", city.Demographics.TotalPopulation.ToString("0")),
            Row("Население", "Дети", city.Demographics.Children.ToString("0")),
            Row("Население", "Взрослые мужчины", city.Demographics.AdultMen.ToString("0")),
            Row("Население", "Взрослые женщины", city.Demographics.AdultWomen.ToString("0")),
            Row("Население", "Старики", city.Demographics.Elderly.ToString("0"))
        };

        foreach (var group in city.Demographics.RaceGroups)
        {
            rows.Add(Row(
                "Население",
                $"Раса: {group.RaceId}",
                group.TotalPopulation.ToString("0"),
                $"Дети {group.Children}, мужчины {group.AdultMen}, женщины {group.AdultWomen}, старики {group.Elderly}"));
        }

        rows.Add(CityWorkforceDiagnosticsRowViewModel.Separator());
        rows.Add(CityWorkforceDiagnosticsRowViewModel.SectionHeader("Трудоспособные"));
        rows.Add(Row("Трудоспособные", "Мужчины", allocation.Workforce.AdultMaleWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.AdultMaleWorkRate:P0}"));
        rows.Add(Row("Трудоспособные", "Женщины", allocation.Workforce.AdultFemaleWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.AdultFemaleWorkRate:P0}"));
        rows.Add(Row("Трудоспособные", "Старики", allocation.Workforce.ElderlyWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.ElderlyWorkRate:P0}"));
        rows.Add(Row("Трудоспособные", "Детский труд", allocation.Workforce.ChildWorkers.ToString("0.##"), $"Ставка участия: {lawProfile.ChildLaborRate:P0}"));
        rows.Add(Row("Трудоспособные", "Глобальный модификатор", allocation.Workforce.GlobalWorkforceModifier.ToString("0.##")));
        rows.Add(Row("Трудоспособные", "Всего рабочих", allocation.Workforce.TotalWorkers.ToString("0")));

        rows.Add(CityWorkforceDiagnosticsRowViewModel.Separator());
        rows.Add(CityWorkforceDiagnosticsRowViewModel.SectionHeader("Распределение по секторам"));
        rows.Add(CityWorkforceDiagnosticsRowViewModel.Message("Preview рассчитан по текущему состоянию города. Производство меняется отдельной логикой симуляции.", "Распределение по секторам"));
        rows.Add(Row("Распределение по секторам", "Земледелие", allocation.AgricultureWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Рыбалка", allocation.FishingWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Охота", allocation.HuntingWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Добыча ресурсов", allocation.ResourceGatheringWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Крафтинг", allocation.CraftingWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Торговля", allocation.TradeWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Стража", allocation.GuardWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Обслуживание", allocation.MaintenanceWorkers.ToString("0")));
        rows.Add(Row("Распределение по секторам", "Свободные / idle", allocation.IdleWorkers.ToString("0")));

        rows.Add(CityWorkforceDiagnosticsRowViewModel.Separator());
        rows.Add(CityWorkforceDiagnosticsRowViewModel.SectionHeader("Локальные лимиты секторов"));
        rows.Add(Row("Локальные лимиты секторов", "Земледелие", capacity.AgricultureCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Рыбалка", capacity.FishingCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Охота", capacity.HuntingCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Добыча ресурсов", capacity.ResourceGatheringCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Крафтинг", capacity.CraftingCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Торговля", capacity.TradeCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Стража", capacity.GuardCapacity.ToString("0")));
        rows.Add(Row("Локальные лимиты секторов", "Обслуживание", capacity.MaintenanceCapacity.ToString("0")));

        return rows;
    }

    private static CityWorkforceDiagnosticsRowViewModel Row(string section, string title, string value, string description = "") =>
        new(section, title, value, description, CityWorkforceDiagnosticsRowKind.Value);
}

public sealed record CityWorkforceDiagnosticsRowViewModel(
    string Section,
    string Title,
    string Value,
    string Description,
    CityWorkforceDiagnosticsRowKind RowKind)
{
    public bool IsSeparator => RowKind == CityWorkforceDiagnosticsRowKind.Separator;

    public bool IsSection => RowKind == CityWorkforceDiagnosticsRowKind.Section;

    public bool IsMessage => RowKind == CityWorkforceDiagnosticsRowKind.Message;

    public static CityWorkforceDiagnosticsRowViewModel SectionHeader(string title) =>
        new(title, title, string.Empty, string.Empty, CityWorkforceDiagnosticsRowKind.Section);

    public static CityWorkforceDiagnosticsRowViewModel Separator() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, CityWorkforceDiagnosticsRowKind.Separator);

    public static CityWorkforceDiagnosticsRowViewModel Message(string description, string section = "") =>
        new(section, string.Empty, string.Empty, description, CityWorkforceDiagnosticsRowKind.Message);
}

public enum CityWorkforceDiagnosticsRowKind
{
    Value,
    Section,
    Message,
    Separator
}
