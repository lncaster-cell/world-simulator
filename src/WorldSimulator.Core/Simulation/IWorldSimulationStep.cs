using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public delegate void WorldSimulationStepDelegate();

public interface IWorldSimulationStep
{
    void Execute(
        SimulationWorld world,
        City city,
        int day,
        WorldSimulationContext context,
        WorldSimulationStepDelegate next);
}
