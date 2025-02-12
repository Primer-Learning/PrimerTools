using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public interface ICreatureFactory
{
    ICreatureModelHandler CreateInstance();
}
