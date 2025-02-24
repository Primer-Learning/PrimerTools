using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public delegate int FindMateDelegate(IEnumerable<CreatureComponent> labeledCollisions, Vector3
    creaturePosition);
public delegate CreatureComponent ReproduceDelegate(Genome genome1, Genome genome2, Rng rng);

public static class MateSelectionStrategies
{
    // public static int FindFirstAvailableMate(IEnumerable<CreatureComponent> labeledCollisions, Vector3 creaturePosition)
    // {
    //     return labeledCollisions
    //         .OrderBy(c => (c.Position - creaturePosition).LengthSquared())
    //         .Select(c => c.EntityId)
    //         .FirstOrDefault();
    // }

    // TODO: Re-implement this when it's needed. It doesn't match the current patterns, and it's not needed right now.
    // public static CreatureComponent AsexualFindMate(int creatureIndex, IEnumerable<CreatureComponent> labeledCollisions, Vector3
    //     creaturePosition)
    // {
    //     return creatureIndex;
    // }
}

public static class ReproductionStrategies
{
    public static CreatureComponent AsexualReproduce(Genome genome1, Genome genome2, Rng rng)
    {
        var newGenome = genome1.Clone();
        newGenome.Mutate(rng);
        var newCreature = new CreatureComponent { Genome = newGenome };
        return newCreature;
    }

    public static CreatureComponent SexualReproduce(Genome genome1, Genome genome2, Rng rng)
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
            else if (trait1 is DeleteriousTrait deleteriousTrait1 && trait2 is DeleteriousTrait deleteriousTrait2)
            {
                var currentParentIndex = rng.RangeInt(0, 2);
                var newAlleles = new List<bool>();
                for (var i = 0; i < deleteriousTrait1.Alleles.Count; i++)
                {
                    var parentTrait = parentGenomes[currentParentIndex].Traits[traitName] as DeleteriousTrait;
                    newAlleles.Add(parentTrait.Alleles.RandomItem(rng));
                    currentParentIndex = 1 - currentParentIndex; // Switch to the other parent for the next allele
                }

                newGenome.AddTrait(
                    DeleteriousTrait.CreateNew(
                        deleteriousTrait1.ActivationAge,
                        deleteriousTrait1.MortalityRatePerSecond,
                        newAlleles,
                        traitName
                    )
                );
            }
            else if (trait1 is Trait<bool> boolTrait1 && trait2 is Trait<bool> boolTrait2)
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
            else {GD.Print($"{traitName} not added to new genome");}
        }

        newGenome.Mutate(rng);

        return new CreatureComponent { Genome = newGenome };
    }
}
