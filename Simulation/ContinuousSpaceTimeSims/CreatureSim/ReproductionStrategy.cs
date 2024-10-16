using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public delegate int FindMateDelegate(int creatureIndex, IEnumerable<int> labeledCollisions);
public delegate DataCreature ReproduceDelegate(Genome genome1, Genome genome2, Rng rng);

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
    public static DataCreature AsexualReproduce(Genome genome1, Genome genome2, Rng rng)
    {
        var newGenome = genome1.Clone();
        MutateCreature(newGenome, rng);
        var newCreature = new DataCreature { Genome = newGenome };
        return newCreature;
    }

    public static DataCreature SexualReproduce(Genome genome1, Genome genome2, Rng rng)
    {
        var newGenome = new Genome();

        foreach (var traitName in genome1.Traits.Keys)
        {
            var trait1 = genome1.Traits[traitName];
            var trait2 = genome2.Traits[traitName];

            if (trait1 is Trait<float> floatTrait1 && trait2 is Trait<float> floatTrait2)
            {
                var newAlleles = new List<float>(); 
                for (var i = 0; i < floatTrait1.Alleles.Count; i++)
                {
                    newAlleles.Add(rng.RangeFloat(0, 1) < 0.5 ? floatTrait1.Alleles[i] : floatTrait2.Alleles[i]);
                }

                newGenome.AddTrait(new Trait<float>(traitName, newAlleles, floatTrait1.ExpressionMechanism, floatTrait1.MutationIncrement));
            }
            // Add more type checks for other trait types
        }

        MutateCreature(newGenome, rng);
        var newCreature = new DataCreature { Genome = newGenome };
        return newCreature;
    }

    private static void MutateCreature(Genome genome, Rng rng)
    {
        foreach (var trait in genome.Traits.Values)
        {
            if (trait is Trait<float> floatTrait)
            {
                if (rng.RangeFloat(0, 1) < CreatureSimSettings.MutationProbability)
                {
                    floatTrait.Mutate(rng);
                }
            }
            // Add more type checks for other trait types
        }
    }
}
