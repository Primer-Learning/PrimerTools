using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntity : Node3D
{
    public virtual void Initialize(IDataEntity dataEntity){}
    public virtual void Update(IDataEntity dataEntity){}
}
