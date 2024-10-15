using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public delegate int FindMateDelegate(int creatureIndex, IEnumerable<int> labeledCollisions);
public delegate DataCreature ReproduceDelegate(DataCreature parent1, DataCreature parent2, Rng rng);

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
        
        MutateCreature(ref newCreature, null);
        
        return newCreature;
    }

    public static DataCreature SexualReproduce(DataCreature parent1, DataCreature parent2, Rng rng)
    {
        var newGenome = new Genome();

        foreach (var traitName in parent1.Genome.Traits.Keys)
        {
            var trait1 = parent1.Genome.Traits[traitName];
            var trait2 = parent2.Genome.Traits[traitName];

            if (trait1 is Trait<float> floatTrait1 && trait2 is Trait<float> floatTrait2)
            {
                var newAlleles = new List<float> 
                {
                    rng.RangeFloat(0, 1) < 0.5 ? floatTrait1.Alleles[0] : floatTrait2.Alleles[0]
                };

                newGenome.AddTrait(new Trait<float>(traitName, newAlleles, floatTrait1.ExpressionMechanism, floatTrait1.MutationMechanism));
            }
            // Add more type checks for other trait types
        }

        var newCreature = new DataCreature { Genome = newGenome };
        MutateCreature(ref newCreature, rng);
        
        
        return newCreature;
    }

    private static void MutateCreature(ref DataCreature creature, Rng rng)
    {
        foreach (var trait in creature.Genome.Traits.Values)
        {
            if (trait is Trait<float> floatTrait)
            {
                if (rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
                {
                    floatTrait.Mutate();
                }
            }
            // Add more type checks for other trait types
        }
    }
}
