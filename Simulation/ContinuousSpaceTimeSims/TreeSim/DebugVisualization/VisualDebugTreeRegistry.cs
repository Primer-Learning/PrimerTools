using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class VisualDebugTreeRegistry : IEntityRegistry<IVisualTree>
{
    public VisualDebugTreeRegistry(World3D world3D)
    {
        VisualDebugTree.World3D = world3D;
    }
    public List<IVisualTree> Entities { get; } = new();
    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalTree physicalTree)
        {
            GD.PrintErr($"VisualDebugTreeRegistry was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalTree.");
            return;
        }

        var newTree = new VisualDebugTree();
        newTree.Initialize(physicalTree);
        Entities.Add(newTree);
    }
}