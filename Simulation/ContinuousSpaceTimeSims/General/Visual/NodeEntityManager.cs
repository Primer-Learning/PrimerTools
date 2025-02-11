using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public class NodeEntityManager<TDataEntity> where TDataEntity : IDataEntity
{
    public readonly List<NodeEntity> NodeEntities = new();
    public List<NodeEntity> PreExistingNodeEntities = new();
    protected readonly DataEntityRegistry<TDataEntity> DataEntityRegistry;
    public IReadOnlyList<TDataEntity> DataEntities => DataEntityRegistry.Entities;
    private readonly Node3D _parent;
    private readonly Func<NodeEntity> _entityFactory;

    public NodeEntityManager(
        DataEntityRegistry<TDataEntity> dataEntityRegistry,
        Node3D parent,
        Func<NodeEntity> entityFactory)
    {
        DataEntityRegistry = dataEntityRegistry;
        _parent = parent;
        _entityFactory = entityFactory;
        
        DataEntityRegistry.EntityRegistered += RegisterEntity;
        DataEntityRegistry.EntityUnregistered += RemoveEntity;
        DataEntityRegistry.ResetEvent += Reset;
    }
    
    private void RegisterEntity(TDataEntity dataEntity)
    {
        NodeEntity nodeEntity;
        if (PreExistingNodeEntities.Any())
        {
            GD.PushWarning("Using PreExistingNodeEntities is untested");
            nodeEntity = PreExistingNodeEntities.Last();
            PreExistingNodeEntities.RemoveAt(PreExistingNodeEntities.Count - 1);
        }
        else
        {
            nodeEntity = _entityFactory();
        }
        
        _parent.AddChild(nodeEntity);
        nodeEntity.Initialize(dataEntity);
        NodeEntities.Add(nodeEntity);
    }
    private void RemoveEntity(int index)
    {
        NodeEntities.RemoveAt(index);
        // Don't queuefree here. It can queuefree itself after an animation
        // GetChild(index).QueueFree();
    }

    public T GetNodeEntityByDataID<T>(Rid rid) where T : NodeEntity
    {
        var index = DataEntityRegistry.EntityLookup[rid];
        return NodeEntities[index] as T;
    }

    private void Reset()
    {
        foreach (var child in NodeEntities)
        {
            child.QueueFree();
        }
        NodeEntities.Clear();
    }
    
    public virtual void VisualProcess(double delta)
    {
        if (DataEntityRegistry == null) return;
    
        for (var i = 0; i < DataEntityRegistry.Entities.Count; i++)
        {
            NodeEntities[i].Update(DataEntityRegistry.Entities[i]);
        }
    }

    public void UnsubscribeFromEvents()
    {
        DataEntityRegistry.EntityRegistered -= RegisterEntity;
        DataEntityRegistry.EntityUnregistered -= RemoveEntity;
        DataEntityRegistry.ResetEvent -= Reset;
    }
}
