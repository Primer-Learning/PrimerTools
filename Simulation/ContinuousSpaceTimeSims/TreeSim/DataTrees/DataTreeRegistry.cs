using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class DataTreeRegistry : IEntityRegistry<DataTree>
{
    public DataTreeRegistry(World3D world3D)
    {
        DataTree.World3D = world3D;
    }

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not DataTree physicalTree)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalTree.");
            return;
        }
        
        physicalTree.Initialize();
        TreeLookup.Add(physicalTree.Body, Entities.Count);
        Entities.Add(physicalTree);
    }

    public List<DataTree> Entities { get; } = new();
    public readonly Dictionary<Rid, int> TreeLookup = new();
    
    public void Reset()
    {
        foreach (var tree in Entities) tree.CleanUp();
        
        Entities.Clear();
        TreeLookup.Clear();
    }
}
