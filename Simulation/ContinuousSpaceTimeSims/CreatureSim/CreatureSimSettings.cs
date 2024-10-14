using System;
using Godot;

namespace PrimerTools.Simulation;

[Tool]
public static class CreatureSimSettings
{
	#region Sim parameters
	// Movement
	public const float CreatureStepMaxLength = 10f;
	public const float MaxAccelerationFactor = 0.1f;
	public const float CreatureEatDistance = 2;
	public const float CreatureMateDistance = 2;
	
	// State-based pause durations
	public const float EatDuration = 1.5f;
	public const float MaturationTime = 2f;
	
	// Energy
	private const float BaseEnergySpend = 0.1f;
	private const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	private const float EnergyGainFromFood = 1f;
	public const float ReproductionEnergyThreshold = 2f;
	public const float ReproductionEnergyCost = 1f;
	public const float DefaultHungerThreshold = 4;
	public const float ReproductionDuration = 1;
	
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
	
	public static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / InitialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / InitialAwarenessRadius;
		
		creature.Energy -= (BaseEnergySpend + GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}
	public static event Action<int, Rid, float> CreatureEatEvent; // creatureIndex, treeIndex, duration

	public static DataCreature EatFood(DataCreature creature, ref DataTree tree, int creatureIndex)
	{
		// var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return creature;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		// FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Energy += EnergyGainFromFood;
		creature.EatingTimeLeft = EatDuration;
		CreatureEatEvent?.Invoke(creatureIndex, tree.Body, EatDuration / SimulationWorld.TimeScale);
		return creature;
	}
}

