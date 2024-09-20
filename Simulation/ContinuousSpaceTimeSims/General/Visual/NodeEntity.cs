using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntity<T> : Node3D where T : IDataEntity
{
    public virtual void Initialize(T dataEntity){}
    public virtual void UpdateTransform(T dataEntity){}
    public virtual void Death(){}
}
