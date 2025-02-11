using Godot;

namespace PrimerTools.Simulation;

public interface ICreatureFactory
{
    ICreatureModelHandler CreateInstance();
}
