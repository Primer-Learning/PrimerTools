using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public interface IReproductionStrategy
{
    DataCreature Reproduce(ref DataCreature parent, DataEntityRegistry<DataCreature> registry);
}

public class AsexualReproductionStrategy : IReproductionStrategy
{
    public DataCreature Reproduce(ref DataCreature parentCreature, DataEntityRegistry<DataCreature> registry)
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
    public DataCreature Reproduce(ref DataCreature parent, DataEntityRegistry<DataCreature> registry)
    {
        parent.OpenToMating = true;
        var (closestMateIndex, canMate) = FindClosestPotentialMate(parent, registry);
        if (canMate && parent.MatingTimeLeft <= 0)
        {
            var newCreature = ReproduceSexually(ref parent, closestMateIndex, registry);
            return newCreature;
        }
        else if (closestMateIndex > -1)
        {
            ChooseMateDestination(ref parent, closestMateIndex, registry);
        }

        return default;
    }

    private DataCreature ReproduceSexually(ref DataCreature parent1, int parent2Index, DataEntityRegistry<DataCreature> registry)
    {
        var parent2 = registry.Entities[parent2Index];
		
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
        registry.Entities[parent2Index] = parent2;
		
        return newCreature;
    }

    private (int, bool) FindClosestPotentialMate(DataCreature creature, DataEntityRegistry<DataCreature> registry)
    {
        var labeledCollisions = DataCreatureBehaviorHandler.CreatureSim.GetLabeledAndSortedCollisions(creature);
        var closestMate = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Creature);

        if (closestMate.Type == CollisionType.Creature)
        {
            var canMate = (closestMate.Position - creature.Position).LengthSquared() < DataCreatureBehaviorHandler.CreatureMateDistance * DataCreatureBehaviorHandler.CreatureMateDistance;
            return (closestMate.Index, canMate);
        }

        return (-1, false);
    }

    private void ChooseMateDestination(ref DataCreature creature, int mateIndex, DataEntityRegistry<DataCreature> registry)
    {
        var mate = registry.Entities[mateIndex];
        creature.CurrentDestination = mate.Position;
    }
}
