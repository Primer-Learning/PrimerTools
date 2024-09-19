using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeEntityManager<TEntity, TNode> : Node3D
    where TEntity : IEntity
    where TNode : NodeEntity<TEntity>, new()
{
    private static readonly Dictionary<Type, Type> EntityToNodeMap = new()
    {
        { typeof(DataTree), typeof(NodeTree) },
        { typeof(DataCreature), typeof(NodeCreature) }
    };

    public List<TNode> Entities => GetChildren().OfType<TNode>().ToList();

    public void RegisterEntity(TEntity dataEntity)
    {
        if (!EntityToNodeMap.TryGetValue(typeof(TEntity), out var nodeType))
        {
            throw new InvalidOperationException($"No corresponding Node type found for {typeof(TEntity)}");
        }

        var node = new TNode();
        AddChild(node);
        node.Initialize(dataEntity);
    }

    public void RemoveEntity(int index)
    {
        GetChild(index).QueueFree();
    }

    public void Reset()
    {
        foreach (var child in Entities)
        {
            child.QueueFree();
        }
    }
}
