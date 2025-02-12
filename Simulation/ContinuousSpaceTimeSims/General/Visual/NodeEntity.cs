using Godot;

namespace PrimerTools.Simulation;

public abstract partial class NodeEntity : Node3D
{
    public virtual void Initialize(IDataEntity dataEntity){}
    public virtual void Update(IDataEntity dataEntity){}
}
