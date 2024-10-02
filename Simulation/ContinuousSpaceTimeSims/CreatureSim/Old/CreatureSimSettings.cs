using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation.Old
{

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
	private const float CreatureStepMaxLength = 10f;
	private const float MaxAccelerationFactor = 0.1f;
	private const float CreatureEatDistance = 2;
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
	
    private static void UpdateVelocity(ref DataCreature creature)
	{
		var desiredDisplacement = creature.CurrentDestination - creature.Position;
		var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
		
		// If we're basically there, choose a new destination
		if (desiredDisplacementLengthSquared < CreatureEatDistance * CreatureEatDistance)
		{
			ChooseDestination(ref creature);
			desiredDisplacement = creature.CurrentDestination - creature.Position;
			desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
		}
		
		// Calculate desired velocity
		var desiredVelocity = desiredDisplacement * creature.MaxSpeed / Mathf.Sqrt(desiredDisplacementLengthSquared);
		
		// Calculate velocity change
		var velocityChange = desiredVelocity - creature.Velocity;
		var velocityChangeLengthSquared = velocityChange.LengthSquared();

		// Calculate acceleration vector with a maximum magnitude
		var maxAccelerationMagnitudeSquared = creature.MaxSpeed * creature.MaxSpeed * MaxAccelerationFactor * MaxAccelerationFactor;
		Vector3 accelerationVector;
		if (velocityChangeLengthSquared > maxAccelerationMagnitudeSquared)
		{
			accelerationVector =  Mathf.Sqrt(maxAccelerationMagnitudeSquared / velocityChangeLengthSquared) * velocityChange;
		}
		else
		{
			accelerationVector = velocityChange;
		}

		// Update velocity
		creature.Velocity += accelerationVector;
	}
	public static void UpdatePositionAndVelocity(ref DataCreature creature)
	{
		UpdateVelocity(ref creature);

		// Limit velocity to max speed
		var velocityLengthSquared = creature.Velocity.LengthSquared();
		var maxSpeedSquared = creature.MaxSpeed * creature.MaxSpeed;
		if (velocityLengthSquared > maxSpeedSquared)
		{
			creature.Velocity = creature.MaxSpeed / Mathf.Sqrt(velocityLengthSquared) * creature.Velocity;
		}
		
		// Update position
		creature.Position += creature.Velocity / SimulationWorld.PhysicsStepsPerSimSecond;
	}
	private static void ChooseDestination(ref DataCreature creature)
	{
		Vector3 newDestination;
		var attempts = 0;
		const int maxAttempts = 100;

		do
		{
			var angle = SimulationWorld.Rng.RangeFloat(1) * 2 * Mathf.Pi;
			var displacement = Rng.RangeFloat(1) * CreatureStepMaxLength * new Vector3(
				Mathf.Sin(angle),
				0,
				Mathf.Cos(angle)
			);
			newDestination = creature.Position + displacement;
			attempts++;

			if (attempts >= maxAttempts)
			{
				GD.PrintErr($"Failed to find a valid destination after {maxAttempts} attempts. Using current position.");
				newDestination = creature.Position;
				break;
			}
		} while (!SimulationWorld.IsWithinWorldBounds(newDestination));

		creature.CurrentDestination = newDestination;
	}
	public static void ChooseTreeDestination(ref DataCreature creature, int treeIndex)
	{
		var tree = FruitTreeSim.Registry.Entities[treeIndex];
		creature.CurrentDestination = tree.Position;
	}
	public static (int, bool) FindClosestFood(DataCreature creature)
	{
		var labeledCollisions = CreatureSim.GetLabeledAndSortedCollisions(creature);
		var closestFood = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Tree);

		if (closestFood.Type == CollisionType.Tree)
		{
			var canEat = (closestFood.Position - creature.Position).LengthSquared() < CreatureEatDistance * CreatureEatDistance;
			return (closestFood.Index, canEat);
		}

		return (-1, false);
	}
	
	public static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / InitialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / InitialAwarenessRadius;
		
		creature.Energy -= (BaseEnergySpend + GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}
	public static event Action<int, int, float> CreatureEatEvent; // creatureIndex, treeIndex, duration
	public static event Action<int> CreatureDeathEvent; // creatureIndex

	public static void EatFood(ref DataCreature creature, int treeIndex, int creatureIndex)
	{
		var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Energy += EnergyGainFromFood;
		creature.EatingTimeLeft = EatDuration;

		CreatureEatEvent?.Invoke(creatureIndex, treeIndex, EatDuration / SimulationWorld.TimeScale);
	}

	public static void CheckAndHandleCreatureDeath(ref DataCreature creature, int creatureIndex)
	{
		var alive = creature.Energy > 0;
		alive = alive && creature.Age < creature.MaxAge;
		if (!alive)
		{
			creature.Alive = false;
			CreatureDeathEvent?.Invoke(creatureIndex);
		}
	}
}
}
