using Godot;

namespace PrimerTools.Simulation;

public interface IVisualTree : IEntity
{
    public void UpdateTransform(PhysicalTree physicalTree);
    /// <summary>
    /// Should take care of disposal. It happens here so it can wait for an animation if necessary.
    /// </summary>
    public void Death();

    public bool HasFruit { get; }
    public void GrowFruit(double duration);
    public Node3D GetFruit();
}