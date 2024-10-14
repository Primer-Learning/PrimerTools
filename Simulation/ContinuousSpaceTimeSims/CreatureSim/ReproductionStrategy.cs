using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public delegate int FindMateDelegate(int creatureIndex, IEnumerable<int> labeledCollisions);
public delegate DataCreature ReproduceDelegate(DataCreature parent1, DataCreature parent2);

public static class MateSelectionStrategies
{
    public static int FindFirstAvailableMate(int creatureIndex, IEnumerable<int> labeledCollisions)
    {
        var collisions = labeledCollisions as int[] ?? labeledCollisions.ToArray();
        return collisions.Any() ? collisions.First() : -1;
    }
    public static int AsexualFindMate(int creatureIndex, IEnumerable<int> labeledCollisions)
    {
        return creatureIndex;
    }
}

public static class ReproductionStrategies
{
    // TODO: Make these only accept genomes, once those exist
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
