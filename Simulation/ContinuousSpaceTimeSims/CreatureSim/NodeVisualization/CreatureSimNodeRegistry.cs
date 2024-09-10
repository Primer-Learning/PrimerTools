using System.Collections.Generic;
using Godot;
using PrimerAssets;

namespace PrimerTools.Simulation;

public class CreatureSimNodeRegistry : IEntityRegistry<IVisualCreature>
{
    private CreatureSim _creatureSim;
    public CreatureSimNodeRegistry(CreatureSim creatureSim)
    {
        _creatureSim = creatureSim;
    }
    
    public List<IVisualCreature> Entities { get; } = new();
    
    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr($"CreatureSimNodeRegistry was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalCreature.");
            return;
        }
        
        var creature = new NodeCreature();
        _creatureSim.AddChild(creature);
        Entities.Add(creature);
        creature.Initialize(physicalCreature);
    }
}