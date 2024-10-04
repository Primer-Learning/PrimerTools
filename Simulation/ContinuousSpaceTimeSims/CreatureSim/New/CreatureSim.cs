using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools;

namespace PrimerTools.Simulation.New;

public enum CollisionType
{
	Tree,
	Creature
}

public struct LabeledCollision
{
	public CollisionType Type;
	public int Index;
	public Vector3 Position; // Can be inferred from index and type, but convenient for now.
}

[Tool]
public class CreatureSim : Simulation<DataCreature>
{
	public IReproductionStrategy ReproductionStrategy { get; private set; }
	private IBehaviorStrategy _behaviorStrategy;
	public CreatureSim(SimulationWorld simulationWorld, bool useSexualReproduction = true) : base(simulationWorld)
	{
		ReproductionStrategy = useSexualReproduction ? new SexualReproductionStrategy() : new AsexualReproductionStrategy() as IReproductionStrategy;
		_behaviorStrategy = new SimpleBehaviorStrategy();
	}

	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	protected override void CustomInitialize()
	{
		// TODO: Not this. See comment in CreatureBehaviorHandler
		CreatureSimSettings.FruitTreeSim = FruitTreeSim;
		CreatureSimSettings.CreatureSim = this;
		
		if (FruitTreeSim == null)
		{
			GD.PrintErr("TreeSim not found. Not initializing creature sim because they will all starve to death immediately. You monster.");
			return;
		}
		
		for (var i = 0; i < InitialEntityCount; i++)
		{
			var physicalCreature = new DataCreature
			{
				Position = new Vector3(
					SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
					0,
					SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
				),
				AwarenessRadius = CreatureSimSettings.InitialAwarenessRadius,
				MaxSpeed = CreatureSimSettings.InitialCreatureSpeed,
				MaxAge = CreatureSimSettings.InitialMaxAge
			};

			Registry.RegisterEntity(physicalCreature);
		}
	}
	public List<LabeledCollision> GetLabeledAndSortedCollisions(DataCreature creature)
	{
		var objectsInAwareness = creature.DetectCollisionsWithCreature();
		var labeledCollisions = new List<LabeledCollision>();

		foreach (var objectData in objectsInAwareness)
		{
			var objectRid = (Rid)objectData["rid"];
			if (FruitTreeSim.Registry.EntityLookup.TryGetValue(objectRid, out var treeIndex))
			{
				var tree = FruitTreeSim.Registry.Entities[treeIndex];
				if (tree.HasFruit)
				{
					labeledCollisions.Add(new LabeledCollision
					{
						Type = CollisionType.Tree,
						Index = treeIndex,
						Position = tree.Position
					});
				}
			}
			else if (Registry.EntityLookup.TryGetValue(objectRid, out var creatureIndex))
			{
				var otherCreature = Registry.Entities[creatureIndex];
				if (otherCreature.OpenToMating && otherCreature.Body != creature.Body)
				{
					labeledCollisions.Add(new LabeledCollision
					{
						Type = CollisionType.Creature,
						Index = creatureIndex,
						Position = otherCreature.Position
					});
				}
			}
		}

		labeledCollisions.Sort((a, b) => (a.Position - creature.Position).LengthSquared().CompareTo((b.Position - creature.Position).LengthSquared()));
		return labeledCollisions;
	}

	protected override void CustomStep()
	{
		// Universal updates. More than just age?
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			creature.Age += SimulationWorld.TimeStep;
			Registry.Entities[i] = creature;
		}

		UpdateIntents();
		PerformEating();
		PerformReproductions();
		ProcessMoveActions();
		PerformDeathActions();
	}
	
	private void UpdateIntents()
	{
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			var labeledCollisions = GetLabeledAndSortedCollisions(creature);
			_behaviorStrategy.DetermineAction(i, labeledCollisions, Registry);
		}
	}

	private void PerformEating()
	{
		
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			if (!creature.Actions.HasFlag(ActionFlags.Eat)) continue;

			// TODO: Move this method into this class
			creature = CreatureSimSettings.EatFood(creature, creature.FoodTargetIndex, i);
			
			Registry.Entities[i] = creature;
		}
	}
	
	private void PerformReproductions()
	{
		
	}

	private void ProcessMoveActions()
	{
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];

			if (!creature.Actions.HasFlag(ActionFlags.Move)) continue;

			creature.Velocity = UpdateVelocity(creature.Position, creature.CurrentDestination, creature.Velocity, creature.MaxSpeed);
			creature.Position += creature.Velocity / SimulationWorld.PhysicsStepsPerSimSecond;
			
			var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			CreatureSimSettings.SpendMovementEnergy(ref creature);
			
			Registry.Entities[i] = creature;
		}
	}

	private static Vector3 UpdateVelocity(Vector3 position, Vector3 destination, Vector3 currentVelocity, float maxSpeed)
	{
		var desiredDisplacement = destination - position;
		var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
		
		// If we're basically there, choose a new destination
		if (desiredDisplacementLengthSquared < CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance)
		{
			GD.PushWarning("Creature is already at its destination during UpdateVelocity, which shouldn't happen.");
		}
		
		// Calculate desired velocity
		var desiredVelocity = desiredDisplacement * maxSpeed / Mathf.Sqrt(desiredDisplacementLengthSquared);
		
		// Calculate velocity change
		var velocityChange = desiredVelocity - currentVelocity;
		var velocityChangeLengthSquared = velocityChange.LengthSquared();

		// Calculate acceleration vector with a maximum magnitude
		var maxAccelerationMagnitudeSquared = maxSpeed * maxSpeed * CreatureSimSettings.MaxAccelerationFactor * CreatureSimSettings.MaxAccelerationFactor;
		Vector3 accelerationVector;
		if (velocityChangeLengthSquared > maxAccelerationMagnitudeSquared)
		{
			accelerationVector =  Mathf.Sqrt(maxAccelerationMagnitudeSquared / velocityChangeLengthSquared) * velocityChange;
		}
		else
		{
			accelerationVector = velocityChange;
		}

		var newVelocity = currentVelocity + accelerationVector;
		// Limit velocity to max speed
		var velocityLengthSquared = newVelocity.LengthSquared();
		var maxSpeedSquared = maxSpeed * maxSpeed;
		if (velocityLengthSquared > maxSpeedSquared)
		{
			newVelocity = maxSpeed / Mathf.Sqrt(velocityLengthSquared) * newVelocity;
		}

		return newVelocity;
	}
	
	public static event Action<int> CreatureDeathEvent; // creatureIndex

	private void PerformDeathActions()
	{
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			
			var alive = creature.Energy > 0;
			// alive = alive && creature.Age < creature.MaxAge;
			if (!alive)
			{
				creature.Alive = false;
				CreatureDeathEvent?.Invoke(i);
			}

			Registry.Entities[i] = creature;
		}
	}
}
