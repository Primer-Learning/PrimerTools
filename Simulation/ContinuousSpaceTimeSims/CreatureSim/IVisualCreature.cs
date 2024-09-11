using Godot;

namespace PrimerTools.Simulation;

public interface IVisualCreature : IEntity
{
    // Because I've come back to it twice now:
    // There is no Initialize method declared here, even though all inheritors have it
    // This is because there Initialize is only called in contexts where the
    // more specific implementation of Initialize is known. 

    public void Eat(Node3D food, float duration);
    
    /// <summary>
    /// Should take care of disposal. It happens here so it can wait for an animation if necessary.
    /// </summary>
    public void Death();

    public void UpdateTransform(DataCreature dataCreature);
}