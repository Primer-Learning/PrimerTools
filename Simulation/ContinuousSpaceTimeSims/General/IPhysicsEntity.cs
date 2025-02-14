using Godot;

namespace PrimerTools.Simulation;

public interface IPhysicsEntity
{
    Rid GetBodyRid();
}
