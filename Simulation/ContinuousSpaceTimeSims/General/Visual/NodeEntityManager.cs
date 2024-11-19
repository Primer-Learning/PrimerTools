using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntityManager<TDataEntity, TNodeEntity> : Node3D
    where TDataEntity : IDataEntity
    where TNodeEntity : NodeEntity<TDataEntity>, new()
{
    public readonly List<TNodeEntity> NodeEntities = new();
    public List<TNodeEntity> PreExistingNodeEntities = new();
    protected readonly DataEntityRegistry<TDataEntity> DataEntityRegistry;

    public NodeEntityManager(){}
    public NodeEntityManager(DataEntityRegistry<TDataEntity> dataEntityRegistry)
    {
        DataEntityRegistry = dataEntityRegistry;
        DataEntityRegistry.EntityRegistered += RegisterEntity;
        DataEntityRegistry.EntityUnregistered += RemoveEntity;
        DataEntityRegistry.ResetEvent += Reset;
    }
    
    private void RegisterEntity(TDataEntity dataEntity)
    {
        TNodeEntity nodeEntity;
        if (PreExistingNodeEntities.Any())
        {
            GD.PushWarning("Using PreExistingNodeEntities is untested");
            nodeEntity = PreExistingNodeEntities.Last();
            PreExistingNodeEntities.RemoveAt(PreExistingNodeEntities.Count - 1);
        }
        else
        {
            nodeEntity = new TNodeEntity();
        }
        
        AddChild(nodeEntity);
        nodeEntity.Initialize(dataEntity);
        NodeEntities.Add(nodeEntity);
    }
    private void RemoveEntity(int index)
    {
        NodeEntities.RemoveAt(index);
        // Don't queuefree here. It can queuefree itself after an animation
        // GetChild(index).QueueFree();
    }

    public TNodeEntity GetNodeEntityByDataID(Rid rid)
    {
        var index = DataEntityRegistry.EntityLookup[rid];
        return NodeEntities[index];
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
    
}
