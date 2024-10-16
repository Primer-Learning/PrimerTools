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
        public static Func<List<float>, float> Codominant => alleles => alleles.Average();
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

    public void Mutate(Rng rng)
    {
        if (typeof(T) == typeof(float))
        {
            var allele = (float)(object)Alleles[0];
            var increment = (float)(object)MutationIncrement;
            allele += rng.RangeFloat(0, 1) < 0.5f ? -increment : increment;
            allele = Math.Max(0, allele);
            Alleles[0] = (T)(object)allele;
        }
        // Add more type-specific mutation logic here if needed
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
            throw new InvalidOperationException("Cannot add traits to a cloned Genome.");
        }
    }

    public Trait<T> GetTrait<T>(string name)
    {
        return (Trait<T>)Traits[name];
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
}