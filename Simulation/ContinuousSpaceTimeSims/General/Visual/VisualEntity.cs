using Godot;

namespace PrimerTools.Simulation;

public abstract partial class VisualEntity : Node3D, IVisualEntity
{
    public EntityId EntityId { get; private set; }
    public Node3D RootNode => this;

    // TODO: Consider eliminating this
    // Really, this method's purpose is to tell the visual entity
    // which data entity it represents.
    // This could be handled in the constructor.
    // But it's a lil weird because the factories methods would be affected.
    // It's fine for now.
    public virtual void Initialize(EntityRegistry registry, EntityId entityId)
    {
        EntityId = entityId;
    }

    public abstract void Update(EntityRegistry registry);
    public abstract void AddDebugNodes(AreaPhysicsComponent component);

    // This is for a theoretical future where there are multiple visual components together
    // For example, a creature with an emotion or intention indicator. The indicator might have its own data component.
    // But the visual indicators should be attached to the creature node rather than being an independent visual entity.
    // Leaving this commented rather than deleting to record the idea that visual components
    // can be composed into visual entities.
    // protected readonly List<IVisualComponent> VisualComponents = new();
    // protected void AddVisualComponent(IVisualComponent component)
    // {
    //     component.Initialize(this);
    //     VisualComponents.Add(component);
    // }
    // public interface IVisualComponent
    // {
    //     void Initialize(Node3D parent);
    //     void Update(EntityRegistry registry);
    // }
}
