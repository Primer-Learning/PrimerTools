using Godot;

namespace PrimerTools.Simulation;

public interface IVisualCreature : IEntity
{
    public void Eat(Node3D food, float duration);
    
    /// <summary>
    /// Should take care of disposal. It happens here so it can wait for an animation if necessary.
    /// </summary>
    public void Death();

    public void UpdateTransform(PhysicalCreature physicalCreature);
}