﻿using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class NodeCreatureRegistry : IEntityRegistry<NodeCreature>
{
    private readonly CreatureSim _creatureSim;
    public NodeCreatureRegistry(CreatureSim creatureSim)
    {
        _creatureSim = creatureSim;
    }
    
    public List<NodeCreature> Entities { get; } = new();
    
    public void RegisterEntity(IEntity entity)
    {
        if (entity is not DataCreature physicalCreature)
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