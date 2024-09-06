using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class CreatureSimNodeRegistry : ICreatureVisualizer
{
    private CreatureSim _creatureSim;
    public CreatureSimNodeRegistry(CreatureSim creatureSim)
    {
        _creatureSim = creatureSim;
    }
    
    public List<IEntity> Entities { get; } = new();
    
    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimNodeRegistry was passed the wrong kind of entity");
            return;
        }
        
        var creature = new Creature();
        _creatureSim.AddChild(creature);
        creature.Position = physicalCreature.Position;
        creature.Name = "Creature"; 
        Entities.Add(creature);
        // creature.Owner = creatureSim.GetTree().EditedSceneRoot;
    }

    public void Reset()
    {
        foreach (var creature in Entities) creature.Dispose();
        Entities.Clear();
    }

    public void CreatureEat(int index, Node3D food)
    {
        ((Creature)Entities[index]).Eat(food);
    }

    public void UpdateVisualCreature(int i, IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimNodeRegistry was passed the wrong kind of entity");
            return;
        }
        
        var nodeCreature = (Creature)Entities[i];
        nodeCreature.Position = physicalCreature.Position;
					
        // Calculate and apply rotation
        var direction = physicalCreature.Velocity;
        if (direction.LengthSquared() > 0.0001f)
        {
            nodeCreature.LookAt(nodeCreature.GlobalPosition - direction, Vector3.Up);
        }
    }
}