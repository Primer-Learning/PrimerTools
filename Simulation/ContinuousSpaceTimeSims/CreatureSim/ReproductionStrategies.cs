using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public interface IReproductionStrategy
{
    DataCreature Reproduce(ref DataCreature parent);
}

public class AsexualReproductionStrategy : IReproductionStrategy
{
    public DataCreature Reproduce(ref DataCreature parentCreature)
    {
        parentCreature.Energy -= DataCreatureBehaviorHandler.ReproductionEnergyCost;

        var newCreature = parentCreature;
		
        if (SimulationWorld.Rng.RangeFloat(0, 1) < DataCreatureBehaviorHandler.MutationProbability)
        {
            newCreature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? DataCreatureBehaviorHandler.MutationIncrement : -DataCreatureBehaviorHandler.MutationIncrement;
            newCreature.AwarenessRadius = Mathf.Max(0, newCreature.AwarenessRadius);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < DataCreatureBehaviorHandler.MutationProbability)
        {
            newCreature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? DataCreatureBehaviorHandler.MutationIncrement : -DataCreatureBehaviorHandler.MutationIncrement;
            newCreature.MaxSpeed = Mathf.Max(0, newCreature.MaxSpeed);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < DataCreatureBehaviorHandler.MutationProbability)
        {
            newCreature.MaxAge += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? DataCreatureBehaviorHandler.MutationIncrement : -DataCreatureBehaviorHandler.MutationIncrement;
            newCreature.MaxAge = Mathf.Max(0, newCreature.MaxAge);
        }


        return newCreature;
    }
}

public class SexualReproductionStrategy : IReproductionStrategy
{
    private readonly CreatureSim _creatureSim;

    public SexualReproductionStrategy(CreatureSim creatureSim)
    {
        _creatureSim = creatureSim;
    }

    public DataCreature Reproduce(ref DataCreature parent)
    {
        parent.OpenToMating = true;
        var (closestMateIndex, canMate) = DataCreatureBehaviorHandler.FindClosestPotentialMate(parent);
        if (canMate && parent.MatingTimeLeft <= 0)
        {
            var newCreature = ReproduceSexually(ref parent, closestMateIndex);
            return newCreature;
        }
        else if (closestMateIndex > -1)
        {
            DataCreatureBehaviorHandler.ChooseMateDestination(ref parent, closestMateIndex);
        }

        return default;
    }

    public DataCreature ReproduceSexually(ref DataCreature parent1, int parent2Index)
    {
        var parent2 = _creatureSim.Registry.Entities[parent2Index];
		
        parent1.Energy -= DataCreatureBehaviorHandler.ReproductionEnergyCost / 2;
        parent2.Energy -= DataCreatureBehaviorHandler.ReproductionEnergyCost / 2;
		
        var newCreature = parent1;

        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
        {
            newCreature.AwarenessRadius = parent2.AwarenessRadius;
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
        {
            newCreature.MaxSpeed = parent2.MaxSpeed;
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
        {
            newCreature.MaxAge = parent2.MaxAge;
        }
		
        if (SimulationWorld.Rng.RangeFloat(0, 1) < DataCreatureBehaviorHandler.MutationProbability)
        {
            newCreature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? DataCreatureBehaviorHandler.MutationIncrement : -DataCreatureBehaviorHandler.MutationIncrement;
            newCreature.AwarenessRadius = Mathf.Max(0, newCreature.AwarenessRadius);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < DataCreatureBehaviorHandler.MutationProbability)
        {
            newCreature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? DataCreatureBehaviorHandler.MutationIncrement : -DataCreatureBehaviorHandler.MutationIncrement;
            newCreature.MaxSpeed = Mathf.Max(0, newCreature.MaxSpeed);
        }
        if (SimulationWorld.Rng.RangeFloat(0, 1) < DataCreatureBehaviorHandler.MutationProbability)
        {
            newCreature.MaxAge += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? DataCreatureBehaviorHandler.MutationIncrement : -DataCreatureBehaviorHandler.MutationIncrement;
            newCreature.MaxAge = Mathf.Max(0, newCreature.MaxAge);
        }

        parent2.OpenToMating = false;
        parent1.OpenToMating = false;
        _creatureSim.Registry.Entities[parent2Index] = parent2;
		
        return newCreature;
    }

}
