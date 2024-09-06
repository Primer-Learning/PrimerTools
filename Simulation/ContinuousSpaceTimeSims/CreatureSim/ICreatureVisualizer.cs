using Godot;

namespace PrimerTools.Simulation;

public interface ICreatureVisualizer : IEntityRegistry
{
    public void UpdateVisualCreature(int i, IEntity entity);
}