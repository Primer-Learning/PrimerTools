using System.Collections.Generic;
using Godot;
using PrimerAssets;

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
        creature.Scale = Vector3.Zero;
        creature.Name = "Creature"; 
        Entities.Add(creature);
        // creature.Owner = creatureSim.GetTree().EditedSceneRoot;
        
        creature.AdjustVisualsToCreatureAttributes(physicalCreature);
    }

    public void CreatureEat(int i, Node3D food, float eatDuration)
    {
        ((NodeCreature)Entities[i]).Eat(food, eatDuration);
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
        await tween.ToSignal(tween, Tween.SignalName.Finished);
        creature.CleanUp();
    }
    
    public void UpdateVisualCreature(int i, IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimNodeRegistry was passed the wrong kind of entity");
            return;
        }

        var nodeCreature = (NodeCreature)Entities[i];
        var scaleFactor = Mathf.Min(1, physicalCreature.Age / CreatureSim.MaturationTime);
        nodeCreature.Scale = scaleFactor * Vector3.One;
        
        if (physicalCreature.EatingTimeLeft > 0) return;
        
        // Position and rotation
        nodeCreature.Position = physicalCreature.Position;
        var direction = physicalCreature.Velocity;
        if (direction.LengthSquared() > 0.0001f)
        {
            nodeCreature.LookAt(nodeCreature.GlobalPosition - direction, Vector3.Up);
        }
    }
}