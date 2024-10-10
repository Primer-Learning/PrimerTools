using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation.New;

public delegate int FindMateDelegate(int creatureIndex, List<LabeledCollision> labeledCollisions);
public delegate DataCreature ReproduceDelegate(DataCreature parent1, DataCreature parent2);

public class ReproductionStrategy
{
    public FindMateDelegate FindMate { get; }
    public ReproduceDelegate Reproduce { get; }

    public ReproductionStrategy(FindMateDelegate findMate, ReproduceDelegate reproduce)
    {
        FindMate = findMate;
        Reproduce = reproduce;
    }
}

public static class ReproductionStrategies
{
    public static int FindFirstAvailableMate(int creatureIndex, List<LabeledCollision> labeledCollisions)
    {
        var mateCollisions = labeledCollisions.Where(c => c.Type == CollisionType.Creature).ToList();
        return mateCollisions.Any() ? mateCollisions.First().Index : -1;
    }

    public static int AsexualFindMate(int creatureIndex, List<LabeledCollision> labeledCollisions)
    {
        return creatureIndex;
    }

    public static DataCreature AsexualReproduce(DataCreature parent1, DataCreature parent2)
    {
        parent1.Energy -= CreatureSimSettings.ReproductionEnergyCost;
        var newCreature = parent1;
        
        MutateCreature(ref newCreature);
        
        return newCreature;
    }

    public static DataCreature SexualReproduce(DataCreature parent1, DataCreature parent2)
    {
        var newCreature = parent1;

        // Inherit traits
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
            newCreature.AwarenessRadius = parent2.AwarenessRadius;
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
            newCreature.MaxSpeed = parent2.MaxSpeed;
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
            newCreature.MaxAge = parent2.MaxAge;

        MutateCreature(ref newCreature);
        
        return newCreature;
    }

    private static void MutateCreature(ref DataCreature creature)
    {
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            creature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            creature.AwarenessRadius = Mathf.Max(0, creature.AwarenessRadius);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            creature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            creature.MaxSpeed = Mathf.Max(0, creature.MaxSpeed);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
        {
            creature.MaxAge += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? CreatureSimSettings.MutationIncrement : -CreatureSimSettings.MutationIncrement;
            creature.MaxAge = Mathf.Max(0, creature.MaxAge);
        }
    }
}
