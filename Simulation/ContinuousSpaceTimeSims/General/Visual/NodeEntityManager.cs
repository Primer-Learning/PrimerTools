using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntityManager<TDataEntity, TNodeEntity> : Node3D
    where TDataEntity : IDataEntity
    where TNodeEntity : NodeEntity<TDataEntity>, new()
{
    public readonly List<TNodeEntity> NodeEntities = new();
    private readonly DataEntityRegistry<TDataEntity> _dataEntityRegistry;
    
    public NodeEntityManager(DataEntityRegistry<TDataEntity> dataEntityRegistry)
    {
        _dataEntityRegistry = dataEntityRegistry;
        _dataEntityRegistry.EntityRegistered += RegisterEntity;
        _dataEntityRegistry.EntityUnregistered += RemoveEntity;
        _dataEntityRegistry.ResetEvent += Reset;
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
        if (_dataEntityRegistry == null) return;

        for (var i = 0; i < _dataEntityRegistry.Entities.Count; i++)
        {
            NodeEntities[i].Update(_dataEntityRegistry.Entities[i]);
        }
    }
    
}
