using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public partial class NodeCreatureManager : Node3D
{
    public List<NodeCreature> Entities => GetChildren().OfType<NodeCreature>().ToList();

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not DataCreature dataCreature)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of DataCreature.");
            return;
        }
        
        var creature = new NodeCreature();
        AddChild(creature);
        creature.Initialize(dataCreature);
    }

    public void RemoveCreature(int index)
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