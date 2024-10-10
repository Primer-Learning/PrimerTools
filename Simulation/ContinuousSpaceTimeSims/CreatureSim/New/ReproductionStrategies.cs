using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation.New;

public interface IReproductionStrategy
{
    public int FindMateIndex(int creatureIndex, List<LabeledCollision> labeledCollisions);
    public DataCreature Reproduce(DataCreature parent1, DataCreature parent2);
}

public class AsexualReproductionStrategy : IReproductionStrategy
{
    public int FindMateIndex(int creatureIndex, List<LabeledCollision> labeledCollisions)
    {
        return creatureIndex;
    }
    
    public DataCreature Reproduce(DataCreature parentCreature, DataCreature parent2)
    {
        parentCreature.Energy -= CreatureSimSettings.ReproductionEnergyCost;

        var newCreature = parentCreature;
		
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            newCreature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            newCreature.AwarenessRadius = Mathf.Max(0, newCreature.AwarenessRadius);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            newCreature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            newCreature.MaxSpeed = Mathf.Max(0, newCreature.MaxSpeed);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            newCreature.MaxAge += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            newCreature.MaxAge = Mathf.Max(0, newCreature.MaxAge);
        }
        
        return newCreature;
    }
}

public class SexualReproductionStrategy : IReproductionStrategy
{
    public int FindMateIndex(int creatureIndex, List<LabeledCollision> labeledCollisions)
    {
        labeledCollisions = labeledCollisions
            .Where(c => c.Type == CollisionType.Creature).ToList();

        if (!labeledCollisions.Any()) return -1;
        
        var closestMate = labeledCollisions.First();
        return closestMate.Index;
    }

    public DataCreature Reproduce(DataCreature parent1, DataCreature parent2)
    {
        // Start by copying parent1 
        var newCreature = parent1;

        // Flip coins to see if we inherit from parent 2
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
        {
            newCreature.AwarenessRadius = parent2.AwarenessRadius;
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
        {
            newCreature.MaxSpeed = parent2.MaxSpeed;
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
        {
            newCreature.MaxAge = parent2.MaxAge;
        }
		
        // Check for mutations
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            newCreature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            newCreature.AwarenessRadius = Mathf.Max(0, newCreature.AwarenessRadius);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            newCreature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            newCreature.MaxSpeed = Mathf.Max(0, newCreature.MaxSpeed);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            newCreature.MaxAge += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            newCreature.MaxAge = Mathf.Max(0, newCreature.MaxAge);
        }
        
        return newCreature;
    }
}
