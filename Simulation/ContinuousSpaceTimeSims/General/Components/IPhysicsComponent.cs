using Godot;

namespace PrimerTools.Simulation;

public interface IPhysicsComponent
{
    public Area3D ConstructDebugNode(Node3D parent);
}