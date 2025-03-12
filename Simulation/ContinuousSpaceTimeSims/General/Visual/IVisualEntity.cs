using Godot;

namespace PrimerTools.Simulation;

public interface IVisualEntity
{
    EntityId EntityId { get; }
    Node3D RootNode { get; }
    void Initialize(EntityRegistry registry, EntityId entityId);
    void Update(EntityRegistry registry);
}
