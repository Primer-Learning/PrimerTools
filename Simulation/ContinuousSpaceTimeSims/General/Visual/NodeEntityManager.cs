using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntityManager<TDataEntity, TNodeEntity> : Node3D
    where TDataEntity : IDataEntity
    where TNodeEntity : NodeEntity<TDataEntity>, new()
{
    public readonly List<TNodeEntity> NodeEntities = new();
    private readonly DataEntityRegistry<TDataEntity> DataEntityRegistry;
    
    public NodeEntityManager(DataEntityRegistry<TDataEntity> dataEntityRegistry)
    {
        DataEntityRegistry = dataEntityRegistry;
        DataEntityRegistry.EntityRegistered += RegisterEntity;
        DataEntityRegistry.EntityUnregistered += RemoveEntity;
        DataEntityRegistry.ResetEvent += Reset;
    }
    
    private void RegisterEntity(TDataEntity dataEntity)
    {
        var node = new TNodeEntity();
        AddChild(node);
        node.Initialize(dataEntity);
        NodeEntities.Add(node);
    }
    private void RemoveEntity(int index)
    {
        NodeEntities.RemoveAt(index);
        // Don't queuefree here. It can queuefree itself after an animation
        // GetChild(index).QueueFree();
    }

    private void Reset()
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
            NodeEntities[i].Update(DataEntityRegistry.Entities[i]);
        }
    }
    
}
