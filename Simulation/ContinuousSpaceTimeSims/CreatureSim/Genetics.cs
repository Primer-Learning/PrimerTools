using System;
using System.Collections.Generic;

namespace PrimerTools.Simulation;

public class Trait<T>
{
    public string Name { get; set; }
    public List<T> Alleles { get; set; }
    public Func<List<T>, T> ExpressionMechanism { get; set; }
    public Action<Trait<T>> MutationMechanism { get; set; }
    public T ExpressedValue => ExpressionMechanism(Alleles);

    public Trait(string name, List<T> alleles, Func<List<T>, T> expressionMechanism, Action<Trait<T>> mutationMechanism)
    {
        Name = name;
        Alleles = alleles;
        ExpressionMechanism = expressionMechanism;
        MutationMechanism = mutationMechanism;
    }

    public void Mutate() => MutationMechanism(this);
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
