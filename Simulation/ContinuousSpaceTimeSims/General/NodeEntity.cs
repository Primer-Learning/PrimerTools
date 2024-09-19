using Godot;
using PrimerTools.Simulation;

namespace PrimerTools.Simulation;

public abstract partial class NodeEntity<T> : Node3D where T : IEntity
{
    public abstract void Initialize(T dataEntity);
    public abstract void UpdateTransform(T dataEntity);
    public abstract void Death();
}
