using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation.New;

[Tool]
public static class CreatureSimSettings
{
	// TODO: Not this. There's a better way to have things talk to each other.
	// Possibly have SimulationWorld be the hub for sims talking to each other.
	public static FruitTreeSim FruitTreeSim;
	public static CreatureSim CreatureSim;
	
	// This is less bad, but let's still think about it
	
	#region Sim parameters
	// Movement
	public const float CreatureStepMaxLength = 10f;
	public const float MaxAccelerationFactor = 0.1f;
	public const float CreatureEatDistance = 2;
	public const float CreatureMateDistance = 1;
	
	// State-based pause durations
	public const float EatDuration = 1.5f;
	public const float MaturationTime = 2f;
	
	// Energy
	private const float BaseEnergySpend = 0.1f;
	private const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	private const float EnergyGainFromFood = 1f;
	public const float ReproductionEnergyThreshold = 2f;
	public const float ReproductionEnergyCost = 1f;
	public const float DefaultHungerThreshold = 2;
	
	// Initial population
	public const float InitialCreatureSpeed = 5f;
	public const float InitialAwarenessRadius = 5f;
	public const float InitialMaxAge = 20;
	
	// Mutation
	public const float MutationProbability = 0.1f;
	public const float MutationIncrement = 1f;
	#endregion
	
	public static Vector3 GetRandomDestination(Vector3 position)
	{
		Vector3 newDestination;
		var attempts = 0;
		const int maxAttempts = 100;

		do
		{
			var angle = SimulationWorld.Rng.RangeFloat(1) * 2 * Mathf.Pi;
			var displacement = SimulationWorld.Rng.RangeFloat(1) * CreatureStepMaxLength * new Vector3(
				Mathf.Sin(angle),
				0,
				Mathf.Cos(angle)
			);
			newDestination = position + displacement;
			attempts++;

			if (attempts >= maxAttempts)
			{
				GD.PrintErr($"Failed to find a valid destination after {maxAttempts} attempts. Using current position.");
				newDestination = position;
				break;
			}
		} while (!SimulationWorld.IsWithinWorldBounds(newDestination));

		return newDestination;
	}
	
	public static void ChooseTreeDestination(ref DataCreature creature, int treeIndex)
	{
		var tree = FruitTreeSim.Registry.Entities[treeIndex];
		creature.CurrentDestination = tree.Position;
	}
	// public static (int, bool) FindClosestFood(DataCreature creature)
	// {
	// 	var labeledCollisions = CreatureSim.GetLabeledAndSortedCollisions(creature);
	// 	var closestFood = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Tree);
	//
	// 	if (closestFood.Type == CollisionType.Tree)
	// 	{
	// 		var canEat = (closestFood.Position - creature.Position).LengthSquared() < CreatureEatDistance * CreatureEatDistance;
	// 		return (closestFood.Index, canEat);
	// 	}
	//
	// 	return (-1, false);
	// }
	
	public static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / InitialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / InitialAwarenessRadius;
		
		creature.Energy -= (BaseEnergySpend + GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}
	public static event Action<int, int, float> CreatureEatEvent; // creatureIndex, treeIndex, duration

	public static DataCreature EatFood(DataCreature creature, int treeIndex, int creatureIndex)
	{
		var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return creature;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Energy += EnergyGainFromFood;
		creature.EatingTimeLeft = EatDuration;
		CreatureEatEvent?.Invoke(creatureIndex, treeIndex, EatDuration / SimulationWorld.TimeScale);
		return creature;
	}

	
}

