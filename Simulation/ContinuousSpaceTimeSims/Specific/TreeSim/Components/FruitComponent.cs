using Godot;

namespace PrimerTools.Simulation.Components;

public struct FruitComponent : IComponent
{
    public EntityId EntityId { get; set; }
    
    // Parent tree reference
    public EntityId ParentTreeId;
    public int PositionIndex; // Which position on the tree
    
    // Growth and lifecycle
    // public float Age;
    public float GrowthProgress; // 0-1 scale
    // public bool IsRipe => GrowthProgress >= 1.0f;
    public bool IsAttached; // Whether still attached to tree
    public float DetachedTime; // How long since it fell from tree
    
    // Physics
    public BodyHandler Body;
    
    public FruitComponent(EntityId parentTreeId, int positionIndex)
    {
        ParentTreeId = parentTreeId;
        PositionIndex = positionIndex;
        // Age = 0;
        // GrowthProgress = 0;
        IsAttached = true;
        DetachedTime = 0;
    }

    public void CleanUp()
    {
        Body.CleanUp();
    }
}
