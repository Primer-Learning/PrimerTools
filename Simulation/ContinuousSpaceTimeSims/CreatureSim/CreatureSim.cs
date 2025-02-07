using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

[Tool]
public class CreatureSim : Simulation<DataCreature>
{
	public enum DeathCause
	{
		Starvation,
		Aging
	}
	
	public CreatureSim(SimulationWorld simulationWorld) : base(simulationWorld) {}

	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	protected override void CustomInitialize(IEnumerable<Vector3> initialPositions)
	{
		if (FruitTreeSim == null)
		{
			GD.PrintErr("TreeSim not found. Not initializing creature sim because they will all starve to death immediately. You monster.");
			return;
		}
		
		List<Vector3> posList;
		if (initialPositions == null)
		{
			posList = new List<Vector3>();
			for (var i = 0; i < InitialEntityCount; i++)
			{
				posList.Add(
					new Vector3(
						SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
						0,
						SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
					)
				);
			}
		}
		else
		{
			posList = initialPositions.ToList();
		}

		var j = 0;
		foreach (var creature in CreatureSimSettings.Instance.InitializePopulation(InitialEntityCount, SimulationWorld.Rng))
		{
			var mutableVersion = creature;
			mutableVersion.Position = posList[j];
			mutableVersion.CurrentDestination = posList[j];
			
			Registry.RegisterEntity(mutableVersion);
			j++;
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

			if (creature.Digesting > 0)
			{
				// Could be a DigestionRate setting or even trait?
				var digestAmount = Mathf.Min(creature.Digesting, 0.05f);
				creature.Energy += digestAmount;
				creature.Digesting -= digestAmount;
			}
            
            if (!creature.Alive) continue;
            // Process deaths
            if (creature.Energy < 0)
            {
	            creature.Alive = false;
	            CreatureDeathEvent?.Invoke(i, DeathCause.Starvation);
	            Registry.Entities[i] = creature;
	            continue;
            }
            
            // Check for deaths from max age trait
            var maxAgeTrait = creature.Genome.GetTrait<float>("MaxAge");
            if (maxAgeTrait != null && maxAgeTrait.ExpressedValue < creature.Age)
            {
	            creature.Alive = false;
	            CreatureDeathEvent?.Invoke(i, DeathCause.Aging);
	            Registry.Entities[i] = creature;
	            continue;
            }
            
            // Comments this out when aging reduces efficiency
            // Check for death from deleterious mutations
            foreach (var trait in creature.Genome.Traits.Values)
            {
                if (trait is DeleteriousTrait deleteriousTrait)
                {
                    if (deleteriousTrait.CheckForDeath(creature.Age, SimulationWorld.Rng))
                    {
	                    // GD.Print($"Creature died of deleterious mutation {deleteriousTrait.Id} with onset age {deleteriousTrait.ActivationAge} and severity {deleteriousTrait.MortalityRate}");
	                    creature.Alive = false;
	                    CreatureDeathEvent?.Invoke(i, DeathCause.Aging);
	                    Registry.Entities[i] = creature;
                        break;
                    }
                }
            }
            if (!creature.Alive) continue; // Move to next creature. Can't do it from inside the above loop.

            // Deaths from antagonistic pleiotropy
            var apTrait = creature.Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
            if (apTrait is { ExpressedValue: true } && creature.Age > CreatureSimSettings.Instance.MaturationTime)
            {
	            var apDeathRate = 0.03f;
		        if (SimulationWorld.Rng.rand.NextDouble() < 1 - Mathf.Pow(1 - apDeathRate, 1f / SimulationWorld.PhysicsStepsPerSimSecond))
	            {
		            // GD.Print("Death from antagonistic pleiotropy aging");
		            creature.Alive = false;
		            CreatureDeathEvent?.Invoke(i, DeathCause.Aging);
		            Registry.Entities[i] = creature;
		            continue;
	            }
            }
            
            // Do-nothing conditions 
            if (creature.Age < CreatureSimSettings.Instance.MaturationTime) continue;
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
                var mateIndex = CreatureSimSettings.Instance.FindMate(i, creatureCollisions, creature.Position);
                if (mateIndex != -1)
                {
                    var mate = Registry.Entities[mateIndex];
            
                    if ((mate.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.Instance.CreatureMateDistance))
                    {
                        mate.MatingTimeLeft += CreatureSimSettings.Instance.ReproductionDuration;
                        creature.MatingTimeLeft += CreatureSimSettings.Instance.ReproductionDuration;
                        mate.Energy -= CreatureSimSettings.Instance.ReproductionEnergyCost / 2;
                        creature.Energy -= CreatureSimSettings.Instance.ReproductionEnergyCost / 2;

                        var offspring = CreatureSimSettings.Instance.Reproduce(creature.Genome, mate.Genome, SimulationWorld.Rng);
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
                if ((closestFood.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.Instance.CreatureEatDistance)
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
                CreatureSimSettings.Instance.CreatureEatDistance * CreatureSimSettings.Instance.CreatureEatDistance)
            {
                creature.CurrentDestination = SimulationWorld.GetRandomDestination(creature.Position, CreatureSimSettings.Instance.CreatureStepMaxLength);
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
		var maxAccelerationMagnitudeSquared = maxSpeed * maxSpeed * CreatureSimSettings.Instance.MaxAccelerationFactor * CreatureSimSettings.Instance.MaxAccelerationFactor;
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
	public static event Action<int, DeathCause> CreatureDeathEvent; // creatureIndex
	public static event Action<int, Rid, float> CreatureEatEvent; // creatureIndex, treeBodyRid, duration
	private DataCreature EatFood(DataCreature creature, ref DataTree tree, int creatureIndex)
	{
		// var tree = FruitTreeSim.Registry.Entities[treeIndex];
		if (!tree.HasFruit) return creature;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		// FruitTreeSim.Registry.Entities[treeIndex] = tree;
		
		creature.Digesting += SimulationWorld.Rng.RangeFloat(CreatureSimSettings.Instance.MinEnergyGainFromFood, CreatureSimSettings.Instance.MaxEnergyGainFromFood);
		creature.EatingTimeLeft = CreatureSimSettings.Instance.EatDuration;
		CreatureEatEvent?.Invoke(creatureIndex, tree.Body, CreatureSimSettings.Instance.EatDuration / SimulationWorld.TimeScale);
		return creature;
	}
	private static void SpendMovementEnergy(ref DataCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / CreatureSimSettings.Instance.ReferenceCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / CreatureSimSettings.Instance.ReferenceAwarenessRadius;
		
		
		var energyCost = (CreatureSimSettings.Instance.BaseEnergySpend +
		                  CreatureSimSettings.Instance.GlobalEnergySpendAdjustmentFactor *
		                  (normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius))
		                 / SimulationWorld.PhysicsStepsPerSimSecond;
	
		// Aging lowers efficiency section
		// foreach (var trait in creature.Genome.Traits.Values)
		// {
		//     if (trait is DeleteriousTrait { ExpressedValue: true, MortalityRatePerSecond: > 0 })
		//     {
		// 	    energyCost *= 1 + creature.Age / 100f;
		//     }
		// }
		
		creature.Energy -= energyCost;
	}
}
