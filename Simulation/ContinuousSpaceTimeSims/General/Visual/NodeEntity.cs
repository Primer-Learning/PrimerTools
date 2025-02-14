using Godot;

namespace PrimerTools.Simulation;

public abstract partial class NodeEntity : Node3D
{
    public abstract void Initialize(IDataEntity dataEntity);
    public abstract void Update(IDataEntity dataEntity);
}
