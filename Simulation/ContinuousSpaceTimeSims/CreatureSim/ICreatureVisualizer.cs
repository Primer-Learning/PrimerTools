using Godot;

namespace PrimerTools.Simulation;

public interface ICreatureVisualizer : IEntityRegistry
{
    public void UpdateVisualCreature(int i, IEntity entity);

    public void CreatureEat(int i, Node3D food) {}

    /// <summary>
    /// Should take care of disposal. It happens here so it can wait for an animation if necessary. 
    /// </summary>
    /// <param name="i"></param>
    public void CreatureDeath(int i)
    {
        var creature = (NodeCreature)Entities[i];
        creature.Visible = false;
        creature.CleanUp();
    }
}