using Godot;

namespace PrimerTools.Simulation;

public interface IPhysicsObjectHandler
{
    public Area3D ConstructDebugNode(Node3D parent);
}