using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class PhysicalTreeRegistry : IEntityRegistry<PhysicalTree>
{
    public PhysicalTreeRegistry(World3D world3D)
    {
        PhysicalTree.World3D = world3D;
    }

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalTree physicalTree)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalTree.");
            return;
        }
        
        physicalTree.Initialize();
        TreeLookup.Add(physicalTree.Body, Entities.Count);
        Entities.Add(physicalTree);
    }

    public List<PhysicalTree> Entities { get; } = new();
    public readonly Dictionary<Rid, int> TreeLookup = new();
    
    public void Reset()
    {
        foreach (var tree in Entities) tree.CleanUp();
        
        Entities.Clear();
        TreeLookup.Clear();
    }
}
