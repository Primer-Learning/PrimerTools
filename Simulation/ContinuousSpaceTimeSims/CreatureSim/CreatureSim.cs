using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

[Tool]
public class CreatureSim : Simulation<DataCreature>
{
	private readonly CreatureSimSettings _settings;
	public CreatureSim(SimulationWorld simulationWorld, CreatureSimSettings settings) : base(simulationWorld)
	{
		_settings = settings;
	}

	private FruitTreeSim FruitTreeSim => simulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	protected override void CustomInitialize()
	{
		if (FruitTreeSim == null)
		{
			GD.PrintErr("TreeSim not found. Not initializing creature sim because they will all starve to death immediately. You monster.");
			return;
		}

		foreach (var creature in _settings.InitializePopulation(InitialEntityCount, _settings, simulationWorld.Rng))
		{
			var position = new Vector3(
				simulationWorld.Rng.RangeFloat(simulationWorld.WorldDimensions.X),
				0,
				simulationWorld.Rng.RangeFloat(simulationWorld.WorldDimensions.Y)
			);
			var mutableVersion = creature;
			mutableVersion.Position = position;
			mutableVersion.CurrentDestination = position;
			
			Registry.RegisterEntity(mutableVersion);
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
            
            if (!creature.Alive) continue;
            // Process deaths
            var alive = creature.Energy > 0;
            
            // Check for deaths from max age trait
            var maxAgeTrait = creature.Genome.GetTrait<float>("MaxAge");
            if (maxAgeTrait != null && maxAgeTrait.ExpressedValue < creature.Age)
            {
	            alive = false;
            }
            
            // Check for death from deleterious mutations
            foreach (var trait in creature.Genome.Traits.Values)
            {
                if (trait is DeleteriousTrait deleteriousTrait)
                {
                    if (deleteriousTrait.CheckForDeath(creature.Age, simulationWorld.Rng))
                    {
	                    GD.Print($"Creature died of deleterious mutation {deleteriousTrait.Id} with onset age {deleteriousTrait.ActivationAge} and severity {deleteriousTrait.MortalityRate}");
                        alive = false;
                        break;
                    }
                }
            }

            // Deaths from antagonistic pleiotropy
            var apTrait = creature.Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
            if (apTrait is { ExpressedValue: true } && creature.Age > CreatureSimSettings.MaturationTime)
            {
	            if (simulationWorld.Rng.rand.NextDouble() < 0.05 / SimulationWorld.PhysicsStepsPerSimSecond)
	            {
		            GD.Print("Death from antagonistic pleiotropy aging");
		            alive = false;
	            }
            }

            // Could really do this after each check
            if (!alive)
            {
	            creature.Alive = false;
	            CreatureDeathEvent?.Invoke(i);
	            Registry.Entities[i] = creature;
	            continue;
            }
            
            // Do-nothing conditions 
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
            
            var labeledCollisions = GetLabeledCollisions(creature);
            
            // Check for mating
            if (creature.OpenToMating)
            {
	            var creatureCollisions = labeledCollisions.Where(x => x.Type == CollisionType.Creature);
                var mateIndex = _settings.FindMate(i, creatureCollisions, creature.Position);
                if (mateIndex != -1)
                {
                    var mate = Registry.Entities[mateIndex];
            
                    if ((mate.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.CreatureMateDistance))
                    {
                        mate.MatingTimeLeft += CreatureSimSettings.ReproductionDuration;
                        creature.MatingTimeLeft += CreatureSimSettings.ReproductionDuration;
                        mate.Energy -= CreatureSimSettings.ReproductionEnergyCost / 2;
                        creature.Energy -= CreatureSimSettings.ReproductionEnergyCost / 2;

                        var offspring = _settings.Reproduce(creature.Genome, mate.Genome, simulationWorld.Rng);
                        offspring.Position = (mate.Position + creature.Position) / 2;
                        Registry.RegisterEntity(offspring);
                        Registry.Entities[i] = creature;
                        Registry.Entities[mateIndex] = mate;
                        continue;
                    }
            
                    creature.CurrentDestination = mate.Position;
                    PerformMovement(ref creature);
                    Registry.Entities[i] = creature;
                    continue;
                }
            }

            // Check for eating
            if (creature.Energy < creature.HungerThreshold)
            {
	            var foods = labeledCollisions
		            .Where(c => c.Type == CollisionType.Tree).ToArray();
	            var closestFood = foods.Length > 0
		            ? foods.MinBy(c => (c.Position - creature.Position).LengthSquared())
		            : default;
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

            // Random movement from here
            if ((creature.CurrentDestination - creature.Position).LengthSquared() <
                CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance)
            {
                creature.CurrentDestination = simulationWorld.GetRandomDestination(creature.Position, CreatureSimSettings.CreatureStepMaxLength);
            }
            PerformMovement(ref creature);
            
            Registry.Entities[i] = creature;
		}
	}
	
	#region Collision handling
	public enum CollisionType
	{
		None,
		Tree,
		Creature
	}
	public struct LabeledCollision
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
	private List<LabeledCollision> GetLabeledCollisions(DataCreature creature)
	{
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

		return labeledCollisions;
	}
	#endregion
	
	private static Vector3 UpdateVelocity(Vector3 position, Vector3 destination, Vector3 currentVelocity, float maxSpeed)
	{
		if (destination == Vector3.Zero) GD.Print("Moving to the origin");
		var desiredDisplacement = destination - position;
		var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
		
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
		creature.Velocity = UpdateVelocity(creature.Position, creature.CurrentDestination, creature.Velocity, creature.AdjustedSpeed);
		creature.Position += creature.Velocity / SimulationWorld.PhysicsStepsPerSimSecond;
		var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
		PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
		PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
		SpendMovementEnergy(ref creature);
	}
	public static event Action<int> CreatureDeathEvent; // creatureIndex
	public static event Action<int, Rid, float> CreatureEatEvent; // creatureIndex, treeBodyRid, duration
	private DataCreature EatFood(DataCreature creature, ref DataTree tree, int creatureIndex)
	{
		// var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return creature;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		// FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Energy += simulationWorld.Rng.RangeFloat(CreatureSimSettings.MinEnergyGainFromFood, CreatureSimSettings.MaxEnergyGainFromFood);
		creature.EatingTimeLeft = CreatureSimSettings.EatDuration;
		CreatureEatEvent?.Invoke(creatureIndex, tree.Body, CreatureSimSettings.EatDuration / SimulationWorld.TimeScale);
		return creature;
	}
	private static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / CreatureSimSettings.ReferenceCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / CreatureSimSettings.ReferenceAwarenessRadius;
		
		creature.Energy -= (CreatureSimSettings.BaseEnergySpend + CreatureSimSettings.GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}
}
