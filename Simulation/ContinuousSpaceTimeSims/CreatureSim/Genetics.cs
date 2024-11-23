using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimerTools.Simulation;

public static class ExpressionMechanisms
{
    public static class Float
    {
        public static Func<List<float>, float> HighDominant => alleles => alleles.Max();
        public static Func<List<float>, float> LowDominant => alleles => alleles.Min();
        public static Func<List<float>, float> AverageCodominant => alleles => alleles.Average();
        public static Func<List<float>, float> RandomCodominant(Rng rng)
        {
            return alleles => alleles.RandomItem(rng);
        }
    }

    public static class Bool
    {
        public static Func<List<bool>, bool> Dominant => alleles => alleles.Any(a => a);
        public static Func<List<bool>, bool> Recessive => alleles => alleles.All(a => a);
        
        public static Func<List<bool>, bool> RandomCodominant(Rng rng)
        {
            return alleles => alleles.RandomItem(rng);
        }
    }
}

public class DeleteriousTrait : Trait<bool>
{
    public int RawActivationAge { get; }
    public float AdjustedActivationAge => (RawActivationAge + 1) * 5; // Map to multiples of five
    public int RawMortalityRate { get; }
    public float AdjustedMortalityRate => (RawMortalityRate + 1) / 20f; // Map to multiples of five percent 

    public DeleteriousTrait(string name, List<bool> alleles, int rawActivationAge, int rawMortalityRate) 
        : base($"Deleterious_{name}", alleles, ExpressionMechanisms.Bool.Dominant, false)
    {
        if (alleles.Count != 2)
            throw new ArgumentException("DeleteriousTrait must be diploid (exactly 2 alleles)", nameof(alleles));
            
        RawActivationAge = rawActivationAge;
        RawMortalityRate = rawMortalityRate;
    }

    public bool CheckForDeath(float age, Rng rng)
    {
        if (!ExpressedValue || age < AdjustedActivationAge) return false;
        return rng.rand.NextDouble() < AdjustedMortalityRate / SimulationWorld.PhysicsStepsPerSimSecond;
    }

    // public static DeleteriousTrait CreateNew(Rng rng)
    // {
    //     return new DeleteriousTrait(
    //         Guid.NewGuid(),
    //         new List<bool> { true, false }, // New mutations start heterozygous
    //         activationAge: rng.RangeInt(1, 21) * 5, // 5 second increments
    //         mortalityRate: rng.RangeInt(1, 21) / 20f // 5% increments
    //     );
    // }
    public static DeleteriousTrait CreateNew(int rawActivationAge, int rawMortalityRate)
    {
        return new DeleteriousTrait(
            $"{rawActivationAge}_{rawMortalityRate}",
            new List<bool> { false, false },
            rawActivationAge: rawActivationAge,
            rawMortalityRate: rawMortalityRate
        );
    }
    public static DeleteriousTrait CreateNew(int rawActivationAge, int rawMortalityRate, List<bool> alleles)
    {
        return new DeleteriousTrait(
            $"{rawActivationAge}_{rawMortalityRate}",
            alleles,
            rawActivationAge: rawActivationAge,
            rawMortalityRate: rawMortalityRate
        );
    }

    public override Trait Clone()
    {
        return new DeleteriousTrait(
            Name,
            new List<bool>(Alleles),
            RawActivationAge,
            RawMortalityRate
        );
    }
}

public abstract class Trait
{
    public string Name { get; }

    protected Trait(string name)
    {
        Name = name;
    }

    public abstract Trait Clone();
}

public class Trait<T> : Trait
{
    public List<T> Alleles { get; }
    public Func<List<T>, T> ExpressionMechanism { get; }
    public T MutationIncrement { get; }
    public T ExpressedValue => ExpressionMechanism(Alleles);

    public Trait(string name, List<T> alleles, Func<List<T>, T> expressionMechanism, T mutationIncrement)
        : base(name)
    {
        Alleles = alleles;
        ExpressionMechanism = expressionMechanism;
        MutationIncrement = mutationIncrement;
    }
    
    public override Trait Clone()
    {
        return new Trait<T>(
            Name,
            new List<T>(Alleles), // Create a new list with the same elements
            ExpressionMechanism,
            MutationIncrement
        );
    }
}

public class Genome
{
    public IReadOnlyDictionary<string, Trait> Traits { get; }

    public Genome()
    {
        Traits = new Dictionary<string, Trait>();
    }

    private Genome(Dictionary<string, Trait> traits)
    {
        Traits = traits;
    }

    public void AddTrait<T>(Trait<T> trait)
    {
        if (Traits is Dictionary<string, Trait> mutableTraits)
        {
            mutableTraits[trait.Name] = trait;
        }
        else
        {
            throw new InvalidOperationException("Cannot add traits to a cloned Genome."); // I don't remember what this is. Seems to not do anything?
        }
    }

    public Trait<T> GetTrait<T>(string name)
    {
        Traits.TryGetValue(name, out var theTrait);
        return (Trait<T>)theTrait;
    }

    public Genome Clone()
    {
        var clonedTraits = new Dictionary<string, Trait>();
        foreach (var kvp in Traits)
        {
            clonedTraits[kvp.Key] = kvp.Value.Clone();
        }
        return new Genome(clonedTraits);
    }
    public void Mutate(Rng rng)
    {
        foreach (var trait in Traits.Values)
        {
            switch (trait)
            {
                case Trait<float> floatTrait:
                {
                    for (var i = 0; i < floatTrait.Alleles.Count; i++)
                    {
                        if (rng.RangeFloat(0, 1) < CreatureSimSettings.Instance.MutationProbability)
                        {
                            var allele = floatTrait.Alleles[i];
                            var increment = floatTrait.MutationIncrement;
                            allele += rng.RangeFloat(0, 1) < 0.5f ? -increment : increment;
                            allele = Math.Max(0, allele);
                            floatTrait.Alleles[i] = allele;
                        }
                    }
                    break;
                }
                case DeleteriousTrait dt:
                {
                    for (var i = 0; i < dt.Alleles.Count; i++)
                    {
                        if (rng.RangeFloat(0, 1) < CreatureSimSettings.Instance.DeleteriousMutationRate)
                        {
                            dt.Alleles[i] = !dt.Alleles[i];
                        }
                    }
                    break;
                }
                case Trait<bool> boolTrait:
                {
                    for (var i = 0; i < boolTrait.Alleles.Count; i++)
                    {
                        if (rng.RangeFloat(0, 1) < CreatureSimSettings.Instance.MutationProbability)
                        {
                            boolTrait.Alleles[i] = !boolTrait.Alleles[i];
                        }
                    }
                    break;
                }
            }
        }
    }
}
