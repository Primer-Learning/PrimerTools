using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

[Tool]
public class CreatureSim : Simulation<DataCreature>
{
	private CreatureSimSettings _settings;
	public CreatureSim(SimulationWorld simulationWorld, CreatureSimSettings settings) : base(simulationWorld)
	{
		_settings = settings;
	}

	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	protected override void CustomInitialize()
	{
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
			physicalCreature.CurrentDestination = physicalCreature.Position;

			Registry.RegisterEntity(physicalCreature);
		}
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

		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			var labeledCollisions = GetLabeledAndSortedCollisions(creature);
            
            // Do-nothing conditions 
            if (!creature.Alive) continue;
            if (creature.Age < CreatureSimSettings.MaturationTime) continue;
            if (creature.EatingTimeLeft > 0)
            {
                // TODO: Get rid of EatingTimeLeft. There could be an absolute time that is compared instead.
                // Meaning we don't have to update this manually.
                creature.EatingTimeLeft -= SimulationWorld.TimeStep;
                Registry.Entities[i] = creature;
                continue;
            }
            if (creature.MatingTimeLeft > 0)
            {
                creature.MatingTimeLeft = Mathf.Max(0, creature.MatingTimeLeft - SimulationWorld.TimeStep);
                Registry.Entities[i] = creature;
                continue;
            }
            
            // Check for mating
            if (creature.OpenToMating)
            {
	            var creatureCollisions = labeledCollisions.Where(x => x.Type == CollisionType.Creature).Select(x => x.Index);
                var mateIndex = _settings.FindMate(i, creatureCollisions);
                if (mateIndex != -1)
                {
                    var mate = Registry.Entities[mateIndex];
            
                    if ((mate.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.CreatureMateDistance))
                    {
                        mate.MatingTimeLeft += CreatureSimSettings.ReproductionDuration;
                        creature.MatingTimeLeft += CreatureSimSettings.ReproductionDuration;
                        mate.Energy -= CreatureSimSettings.ReproductionEnergyCost / 2;
                        creature.Energy -= CreatureSimSettings.ReproductionEnergyCost / 2;
                        Registry.RegisterEntity(_settings.Reproduce(creature, mate));
                        Registry.Entities[i] = creature;
                        Registry.Entities[mateIndex] = mate;
                        continue;
                    }
            
                    creature.CurrentDestination = mate.Position;
                    Registry.Entities[i] = creature;
                    continue;
                }
            }

            // Check for eating
            if (creature.Energy < creature.HungerThreshold)
            {
                var closestFood = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Tree);
                if ((closestFood.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.CreatureEatDistance)
                    && creature.EatingTimeLeft <= 0)
                {
                    creature.FoodTargetIndex = closestFood.Index;
                    var tree = FruitTreeSim.Registry.Entities[creature.FoodTargetIndex];
                    creature = EatFood(creature, ref tree, i);
                    FruitTreeSim.Registry.Entities[creature.FoodTargetIndex] = tree;
                    Registry.Entities[i] = creature;
                    continue;
                }
                
                if (closestFood.Type != CollisionType.None)
                {
	                creature.CurrentDestination = closestFood.Position;
	                PerformMovement(ref creature);
	                Registry.Entities[i] = creature;
	                continue;
                }
            }

            if ((creature.CurrentDestination - creature.Position).LengthSquared() <
                CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance)
            {
                creature.CurrentDestination = SimulationWorld.GetRandomDestination(creature.Position, CreatureSimSettings.CreatureStepMaxLength);
            }
            PerformMovement(ref creature);
            
            // Process deaths
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
	
	#region Collision handling
	private enum CollisionType
	{
		None,
		Tree,
		Creature
	}
	private struct LabeledCollision
	{
		public CollisionType Type;
		public int Index;
		public Vector3 Position;

		public LabeledCollision()
		{
			Index = -1;
			Position = default;
			Type = CollisionType.None;
		}
	}
	public Array<Dictionary> DetectCollisionsWithCreature(DataCreature creature)
	{
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.CollideWithBodies = false;
		queryParams.Exclude = new Array<Rid>() { creature.Body };
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(creature.Awareness, 0);
		queryParams.Transform = Transform3D.Identity.Translated(creature.Position);
		
		return PhysicsServer3D.SpaceGetDirectState(Registry.World3D.Space).IntersectShape(queryParams);
	}
	private List<LabeledCollision> GetLabeledAndSortedCollisions(DataCreature creature)
	{
		// TODO: Put areas on a separate collision layer and mask the collisions so they don't look at each other
		// Just a small optimization
		
		var objectsInAwareness = DetectCollisionsWithCreature(creature);
		var labeledCollisions = new List<LabeledCollision>();

		foreach (var objectData in objectsInAwareness)
		{
			var objectRid = (Rid)objectData["rid"];
			if (FruitTreeSim.Registry.EntityLookup.TryGetValue(objectRid, out var treeIndex))
			{
				var tree = FruitTreeSim.Registry.Entities[treeIndex];
				// TODO: Remove this check? This function should just report collisions, and the 
				// creature can decide if it cares about fruit or openness to mating.
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
				// TODO: Remove the OpenToMating check? This function should just report collisions, and the 
				// creature can decide if it cares about fruit or openness to mating.
				
				if (otherCreature.OpenToMating)
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
	#endregion
	
	private static Vector3 UpdateVelocity(Vector3 position, Vector3 destination, Vector3 currentVelocity, float maxSpeed)
	{
		if (destination == Vector3.Zero) GD.Print("Moving to the origin");
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
	private static void PerformMovement(ref DataCreature creature)
	{
		creature.Velocity = UpdateVelocity(creature.Position, creature.CurrentDestination, creature.Velocity, creature.MaxSpeed);
		creature.Position += creature.Velocity / SimulationWorld.PhysicsStepsPerSimSecond;
		var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
		PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
		PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
		SpendMovementEnergy(ref creature);
	}
	public static event Action<int> CreatureDeathEvent; // creatureIndex
	
	public static event Action<int, Rid, float> CreatureEatEvent; // creatureIndex, treeBodyRid, duration

	public static DataCreature EatFood(DataCreature creature, ref DataTree tree, int creatureIndex)
	{
		// var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return creature;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		// FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Energy += CreatureSimSettings.EnergyGainFromFood;
		creature.EatingTimeLeft = CreatureSimSettings.EatDuration;
		CreatureEatEvent?.Invoke(creatureIndex, tree.Body, CreatureSimSettings.EatDuration / SimulationWorld.TimeScale);
		return creature;
	}
	
	private static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / CreatureSimSettings.InitialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / CreatureSimSettings.InitialAwarenessRadius;
		
		creature.Energy -= (CreatureSimSettings.BaseEnergySpend + CreatureSimSettings.GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}
}
