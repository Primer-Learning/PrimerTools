using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

[Tool]
public static class CreatureBehaviorHandler
{
	public enum SexMode
	{
		Asexual,
		Plurisexual,
		Sexual
	}
	public static SexMode CurrentSexMode = SexMode.Sexual;
	
	// TODO: Not this. There's a better way to have things talk to each other.
	// Possibly have SimulationWorld be the hub for sims talking to each other.
	public static FruitTreeSim FruitTreeSim;
	public static CreatureSim CreatureSim;
	
	// This is less bad, but let's still think about it
	public static PhysicsDirectSpaceState3D Space;
	
	#region Sim parameters
	// Movement
	private const float CreatureStepMaxLength = 10f;
	private const float MaxAccelerationFactor = 0.1f;
	private const float CreatureEatDistance = 2;
	private const float CreatureMateDistance = 1;
	
	// State-based pause durations
	public const float EatDuration = 1.5f;
	public const float MaturationTime = 2f;
	
	// Energy
	private const float BaseEnergySpend = 0.1f;
	private const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	private const float EnergyGainFromFood = 1f;
	public const float ReproductionEnergyThreshold = 4f;
	private const float ReproductionEnergyCost = 2f;
	
	// Initial population
	
	public const float InitialCreatureSpeed = 5f;

	public const float InitialAwarenessRadius = 3f;
	
	// Mutation
	private const float MutationProbability = 0.1f;
	private const float MutationIncrement = 1f;
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
	public static void ChooseDestination(ref DataCreature creature)
	{
		Vector3 newDestination;
		int attempts = 0;
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
	public static void ChooseMateDestination(ref DataCreature creature, int mateIndex)
	{
		var mate = CreatureSim.Registry.Entities[mateIndex];
		creature.CurrentDestination = mate.Position;
	}
	
	public static (int, bool) FindClosestFood(DataCreature creature)
	{
		var objectsInAwareness = DetectCollisionsWithCreature(creature);
		var closestFoodIndex = -1;
		var canEat = false;
		var closestFoodSqrDistance = float.MaxValue;

		foreach (var objectData in objectsInAwareness)
		{
			var objectRid = (Rid)objectData["rid"];
			// GD.Print($"Entities in dict: {FruitTreeSim.Registry.TreeLookup.Count}");
			if (!FruitTreeSim.Registry.TreeLookup.TryGetValue(objectRid, out var treeIndex)) continue;
			// GD.Print($"Index is {treeIndex}. Data entities: {FruitTreeSim.Registry.Entities.Count}");
			var tree = FruitTreeSim.Registry.Entities[treeIndex];
			if (!tree.HasFruit) continue;
			
			var sqrDistance = (creature.Position - tree.Position).LengthSquared();
			if (!(sqrDistance < closestFoodSqrDistance)) continue;
			
			closestFoodSqrDistance = sqrDistance;
			closestFoodIndex = treeIndex;
			if (closestFoodSqrDistance < CreatureEatDistance * CreatureEatDistance)
			{
				canEat = true;
			}
		}

		return (closestFoodIndex, canEat);
	}

	public static (int, bool) FindClosestPotentialMate(DataCreature creature)
	{
		var objectsInAwareness = DetectCollisionsWithCreature(creature);
		var closestMateIndex = -1;
		var canMate = false;
		var closestMateSqrDistance = float.MaxValue;

		foreach (var objectData in objectsInAwareness)
		{
			var objectRid = (Rid)objectData["rid"];
			// GD.Print($"Entities in dict: {FruitTreeSim.Registry.TreeLookup.Count}");
			if (!CreatureSim.Registry.CreatureLookup.TryGetValue(objectRid, out var potentialMateIndex)) continue;
			// GD.Print($"Index is {treeIndex}. Data entities: {FruitTreeSim.Registry.Entities.Count}");
			var potentialMate = CreatureSim.Registry.Entities[potentialMateIndex];
			if (!potentialMate.OpenToMating) continue;
			
			var sqrDistance = (creature.Position - potentialMate.Position).LengthSquared();
			if (!(sqrDistance < closestMateSqrDistance)) continue;
			
			closestMateSqrDistance = sqrDistance;
			closestMateIndex = potentialMateIndex;
			if (closestMateSqrDistance < CreatureMateDistance * CreatureMateDistance)
			{
				canMate = true;
			}
		}

		return (closestMateIndex, canMate);
	}

	public static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / InitialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / InitialAwarenessRadius;
		
		creature.Energy -= (BaseEnergySpend + GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}

	public static void EatFood(ref DataCreature creature, int treeIndex)
	{
		var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Energy += EnergyGainFromFood;
		creature.EatingTimeLeft = EatDuration;
	}

	public static DataCreature ReproduceSexually(ref DataCreature parent1, int parent2Index)
	{
		var parent2 = CreatureSim.Registry.Entities[parent2Index];
		
		parent1.Energy -= ReproductionEnergyCost / 2;
		parent2.Energy -= ReproductionEnergyCost / 2;
		
		var newCreature = parent1;

		if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
		{
			newCreature.AwarenessRadius = parent2.AwarenessRadius;
		}
		if (SimulationWorld.Rng.RangeFloat(0, 1) < 0.5)
		{
			newCreature.MaxSpeed = parent2.MaxSpeed;
		}
		
		if (SimulationWorld.Rng.RangeFloat(0, 1) < MutationProbability)
		{
			newCreature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? MutationIncrement : -MutationIncrement;
			newCreature.AwarenessRadius = Mathf.Max(0, newCreature.AwarenessRadius);
		}
		if (SimulationWorld.Rng.RangeFloat(0, 1) < MutationProbability)
		{
			newCreature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? MutationIncrement : -MutationIncrement;
			newCreature.MaxSpeed = Mathf.Max(0, newCreature.MaxSpeed);
		}

		parent2.OpenToMating = false;
		parent1.OpenToMating = false;
		CreatureSim.Registry.Entities[parent2Index] = parent2;
		
		GD.Print("Made a baby");
		
		return newCreature;
	}

	public static DataCreature ReproduceAsexually(ref DataCreature parentCreature)
	{
		parentCreature.Energy -= ReproductionEnergyCost;

		var newCreature = parentCreature;
		
		if (SimulationWorld.Rng.RangeFloat(0, 1) < MutationProbability)
		{
			newCreature.AwarenessRadius += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? MutationIncrement : -MutationIncrement;
			newCreature.AwarenessRadius = Mathf.Max(0, newCreature.AwarenessRadius);
		}
		if (SimulationWorld.Rng.RangeFloat(0, 1) < MutationProbability)
		{
			newCreature.MaxSpeed += SimulationWorld.Rng.RangeFloat(0, 1) < 0.5f ? MutationIncrement : -MutationIncrement;
			newCreature.MaxSpeed = Mathf.Max(0, newCreature.MaxSpeed);
		}

		return newCreature;
	}
	#region Helpers
	private static Array<Dictionary> DetectCollisionsWithCreature(DataCreature creature)
	{
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(creature.Awareness, 0);
		queryParams.Transform = Transform3D.Identity.Translated(creature.Position);

		// Run query and print
		return Space.IntersectShape(queryParams);
	}
	#endregion
}