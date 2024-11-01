using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public delegate int FindMateDelegate(int creatureIndex, IEnumerable<CreatureSim.LabeledCollision> labeledCollisions, Vector3
    creaturePosition);
public delegate DataCreature ReproduceDelegate(Genome genome1, Genome genome2, Rng rng);

public static class MateSelectionStrategies
{
    public static int FindFirstAvailableMate(int creatureIndex, IEnumerable<CreatureSim.LabeledCollision> labeledCollisions,
        Vector3 creaturePosition)
    {
        return labeledCollisions
            .Where(c => c.Type == CreatureSim.CollisionType.Creature)
            .OrderBy(c => (c.Position - creaturePosition).LengthSquared())
            .Select(c => c.Index)
            .FirstOrDefault(-1);
    }

    public static int AsexualFindMate(int creatureIndex, IEnumerable<CreatureSim.LabeledCollision> labeledCollisions, Vector3
        creaturePosition)
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
        var parentGenomes = new[] { genome1.Clone(), genome2.Clone() };

        // Handle standard float and bool traits
        // These are traits every creature has
        foreach (var traitName in genome1.Traits.Keys.Intersect(genome2.Traits.Keys))
        {
            var trait1 = genome1.Traits[traitName];
            var trait2 = genome2.Traits[traitName];

            if (trait1 is Trait<float> floatTrait1 && trait2 is Trait<float> floatTrait2)
            {
                var currentParentIndex = rng.RangeInt(0, 2);
                var newAlleles = new List<float>();
                for (var i = 0; i < floatTrait1.Alleles.Count; i++)
                {
                    var parentTrait = parentGenomes[currentParentIndex].Traits[traitName] as Trait<float>;
                    newAlleles.Add(parentTrait.Alleles.RandomItem(rng));
                    currentParentIndex = 1 - currentParentIndex; // Switch to the other parent
                }

                newGenome.AddTrait(new Trait<float>(traitName, newAlleles, floatTrait1.ExpressionMechanism, floatTrait1.MutationIncrement));
            }
            
            if (trait1 is Trait<bool> boolTrait1 && trait2 is Trait<bool> boolTrait2)
            {
                var currentParentIndex = rng.RangeInt(0, 2);
                var newAlleles = new List<bool>();
                for (var i = 0; i < boolTrait1.Alleles.Count; i++)
                {
                    var parentTrait = parentGenomes[currentParentIndex].Traits[traitName] as Trait<bool>;
                    newAlleles.Add(parentTrait.Alleles.RandomItem(rng));
                    currentParentIndex = 1 - currentParentIndex; // Switch to the other parent
                }

                newGenome.AddTrait(new Trait<bool>(traitName, newAlleles, boolTrait1.ExpressionMechanism, boolTrait1.MutationIncrement));
            }
        }

        // Handle deleterious traits from both parents
        var allDeleteriousTraits = genome1.Traits.Values
            .Concat(genome2.Traits.Values)
            .OfType<DeleteriousTrait>()
            .Where(x => x.Alleles.Any(v => v))
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (id, traits) in allDeleteriousTraits)
        {
            if (traits.Count == 2) // Both parents have the trait
            {
                var parent1Trait = traits[0];
                var parent2Trait = traits[1];
                
                var newAlleles = new List<bool>
                {
                    parent1Trait.Alleles[rng.RangeInt(0, 2)],
                    parent2Trait.Alleles[rng.RangeInt(0, 2)]
                };
                
                newGenome.AddTrait(new DeleteriousTrait(
                    id,
                    newAlleles,
                    parent1Trait.ActivationAge,
                    parent1Trait.MortalityRate
                ));
            }
            else // Only one parent has the trait
            {
                var parentTrait = traits[0];
                var newAlleles = new List<bool>
                {
                    parentTrait.Alleles[rng.RangeInt(0, 2)],
                    false // Wild-type allele from the other parent
                };
                
                newGenome.AddTrait(new DeleteriousTrait(
                    id,
                    newAlleles,
                    parentTrait.ActivationAge,
                    parentTrait.MortalityRate
                ));
            }
        }

        MutateCreature(newGenome, rng);

        return new DataCreature { Genome = newGenome };
    }

    private static void MutateCreature(Genome genome, Rng rng)
    {
        foreach (var trait in genome.Traits.Values)
        {
            if (rng.RangeFloat(0, 1) < CreatureSimSettings.Instance.MutationProbability)
            {
                if (trait is Trait<float> floatTrait)
                {
                    floatTrait.Mutate(rng);
                }
            }
        }
        // Possibly add new deleterious mutation
        if (rng.RangeFloat(0, 1) < CreatureSimSettings.Instance.DeleteriousMutationRate)
        {
            genome.AddTrait(DeleteriousTrait.CreateNew(rng));
        }
    }
}
