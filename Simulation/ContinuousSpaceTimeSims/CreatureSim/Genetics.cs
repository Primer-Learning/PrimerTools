using System;
using System.Collections.Generic;

namespace PrimerTools.Simulation;

public class Trait<T>
{
    public string Name { get; set; }
    public List<T> Alleles { get; set; }
    public Func<List<T>, T> ExpressionMechanism { get; set; }
    public T MutationIncrement { get; set; }
    public T ExpressedValue => ExpressionMechanism(Alleles);

    public Trait(string name, List<T> alleles, Func<List<T>, T> expressionMechanism, T mutationIncrement)
    {
        Name = name;
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
}

public class Genome
{
    public Dictionary<string, object> Traits { get; } = new Dictionary<string, object>();

    public void AddTrait<T>(Trait<T> trait)
    {
        Traits[trait.Name] = trait;
    }

    public Trait<T> GetTrait<T>(string name)
    {
        return (Trait<T>)Traits[name];
    }
}
