using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeTreeManager : Node3D
{
    public List<NodeTree> Entities => GetChildren().OfType<NodeTree>().ToList();

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not DataTree dataTree)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of DataTree.");
            return;
        }
        
        var tree = new NodeTree();
        AddChild(tree);
        tree.Initialize(dataTree);
    }

    public void RemoveTree(int index)
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
