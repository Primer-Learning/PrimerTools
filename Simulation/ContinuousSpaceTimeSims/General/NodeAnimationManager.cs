using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public abstract partial class NodeAnimationManager<TDataEntity, TNodeEntity> : Node3D
    where TDataEntity : IDataEntity
    where TNodeEntity : NodeEntity<TDataEntity>, new()
{
    private static readonly Dictionary<Type, Type> EntityToNodeMap = new()
    {
        { typeof(DataTree), typeof(NodeTree) },
        { typeof(DataCreature), typeof(NodeCreature) }
    };
    
    protected SimulationWorld SimulationWorld;
    // public NodeEntityRegistry<TDataEntity, TNodeEntity> VisualRegistry;
    public readonly List<TNodeEntity> Entities = new();
    
    public void RegisterEntity(TDataEntity dataEntity)
    {
        if (!EntityToNodeMap.TryGetValue(typeof(TDataEntity), out var nodeType))
        {
            throw new InvalidOperationException($"No corresponding Node type found for {typeof(TDataEntity)}");
        }

        var node = new TNodeEntity();
        AddChild(node);
        node.Initialize(dataEntity);
        Entities.Add(node);
    }
    
    public void RemoveEntity(int index)
    {
        Entities.RemoveAt(index);
        // Don't queuefree here. It can queuefree itself after an animation
        // GetChild(index).QueueFree();
    }

    public void Reset()
    {
        foreach (var child in Entities)
        {
            child.QueueFree();
        }
        Entities.Clear();
    }
    // public void Reset()
    // {
    //     if (VisualRegistry is not null)
    //     {
    //         if (IsInstanceValid(VisualRegistry)) VisualRegistry.Free();
    //     }
    // }

    protected NodeAnimationManager(SimulationWorld simulationWorld)
    {
        SimulationWorld = simulationWorld;
    }

    protected NodeAnimationManager()
    {
    }

    // public virtual void Initialize()
    // {
    //     VisualRegistry = new NodeEntityRegistry<TDataEntity, TNodeEntity>();
    //     AddChild(VisualRegistry);
    // }

    public abstract void VisualProcess(double delta);

    public virtual void CreateVisualEntity(TDataEntity dataEntity)
    {
        RegisterEntity(dataEntity);
    }
    
}