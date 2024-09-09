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
        
        var creature = new NodeCreature();
        _creatureSim.AddChild(creature);
        creature.Position = physicalCreature.Position;
        creature.Name = "Creature"; 
        Entities.Add(creature);
        // creature.Owner = creatureSim.GetTree().EditedSceneRoot;
    }

    public void CreatureEat(int i, Node3D food)
    {
        ((NodeCreature)Entities[i]).Eat(food);
    }
    
    public async void CreatureDeath(int i)
    {
        
        var creature = (NodeCreature)Entities[i];
        var tween = creature.CreateTween();
        tween.TweenProperty(
            creature,
            "scale",
            Vector3.Zero,
            0.5f
        );
        await tween.ToSignal(tween, "finished");
        creature.Dispose();
        
        // DeathAnimation(i, 0.5f);
    }

    private async void DeathAnimation(int i, float delay)
    {
        
    }

    public void UpdateVisualCreature(int i, IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimNodeRegistry was passed the wrong kind of entity");
            return;
        }
        
        var nodeCreature = (NodeCreature)Entities[i];
        nodeCreature.Position = physicalCreature.Position;
					
        // Calculate and apply rotation
        var direction = physicalCreature.Velocity;
        if (direction.LengthSquared() > 0.0001f)
        {
            nodeCreature.LookAt(nodeCreature.GlobalPosition - direction, Vector3.Up);
        }
    }
}