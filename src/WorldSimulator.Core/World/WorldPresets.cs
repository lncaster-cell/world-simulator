using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.World;

public static class WorldPresets
{
    public static SimulationWorld CreateDefaultWorld()
    {
        return new SimulationWorld
        {
            Cities =
            [
                CityPresets.CreateGotha(),
                CityPresets.CreateHighrock(),
                CityPresets.CreateMlynek(),
                CityPresets.CreateWardmark(),
                CityPresets.CreateRivenstal(),
                CityPresets.CreateGavern(),
                CityPresets.CreateBrno(),
                CityPresets.CreateWodenz(),
                CityPresets.CreateThokurRus()
            ],
            SelectedCityId = "gotha"
        };
    }
}
