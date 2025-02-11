using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public abstract partial class NodeEntityManager<TDataEntity> : Node3D, INodeEntityManager 
    where TDataEntity : IDataEntity
{
    protected readonly List<NodeEntity> NodeEntities = new();
    public List<NodeEntity> PreExistingNodeEntities = new();
    protected readonly DataEntityRegistry<TDataEntity> DataEntityRegistry;
    public IReadOnlyList<TDataEntity> DataEntities => DataEntityRegistry.Entities;
    private readonly Func<NodeEntity> _entityFactory;

    protected NodeEntityManager(
        DataEntityRegistry<TDataEntity> dataEntityRegistry,
        Func<NodeEntity> entityFactory)
    {
        DataEntityRegistry = dataEntityRegistry;
        _entityFactory = entityFactory;
        
        DataEntityRegistry.EntityRegistered += RegisterEntity;
        DataEntityRegistry.EntityUnregistered += RemoveEntity;
        DataEntityRegistry.ResetEvent += Reset;
    }
    public NodeEntityManager() {}  

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
        
        AddChild(nodeEntity);
        nodeEntity.Initialize(dataEntity);
        NodeEntities.Add(nodeEntity);
    }

    private void RemoveEntity(int index)
    {
        NodeEntities.RemoveAt(index);
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
    
    // You might think the nodes should just run their own update method from _Process
    // But since the node entities are structs, it's hard to keep references to them.
    // It works well to just do it from here.
    public virtual void VisualProcess(double delta)
    {
        if (DataEntityRegistry == null) return;
    
        for (var i = 0; i < DataEntityRegistry.Entities.Count; i++)
        {
            NodeEntities[i].Update(DataEntityRegistry.Entities[i]);
        }
    }

    protected void UnsubscribeFromEvents()
    {
        DataEntityRegistry.EntityRegistered -= RegisterEntity;
        DataEntityRegistry.EntityUnregistered -= RemoveEntity;
        DataEntityRegistry.ResetEvent -= Reset;
    }

    public override void _Ready()
    {
        base._Ready();
        Name = $"Node{GetType().Name.Replace("Manager", "")}";
    }

    public override void _ExitTree()
    {
        UnsubscribeFromEvents();
        base._ExitTree();
    }

    public new void QueueFree()
    {
        base.QueueFree();
    }
}
