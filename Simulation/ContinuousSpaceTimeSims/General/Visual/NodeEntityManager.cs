using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntityManager<TDataEntity, TNodeEntity> : Node3D
    where TDataEntity : IDataEntity
    where TNodeEntity : NodeEntity<TDataEntity>, new()
{
    public DataEntityRegistry<TDataEntity> DataEntityRegistry;
    public readonly List<TNodeEntity> NodeEntities = new();
    
    public void RegisterEntity(TDataEntity dataEntity)
    {
        var node = new TNodeEntity();
        AddChild(node);
        node.Initialize(dataEntity);
        NodeEntities.Add(node);
    }
    public void RemoveEntity(int index)
    {
        NodeEntities.RemoveAt(index);
        // Don't queuefree here. It can queuefree itself after an animation
        // GetChild(index).QueueFree();
    }

    public void Reset()
    {
        foreach (var child in NodeEntities)
        {
            child.QueueFree();
        }
        NodeEntities.Clear();
    }
    
    public void VisualProcess(double delta)
    {
        if (DataEntityRegistry == null) return;

        for (var i = 0; i < DataEntityRegistry.Entities.Count; i++)
        {
            NodeEntities[i].UpdateTransform(DataEntityRegistry.Entities[i]);
        }
    }
    
}