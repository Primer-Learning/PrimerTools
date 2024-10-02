using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public enum CollisionType
{
	Tree,
	Creature
}

public struct LabeledCollision
{
	public CollisionType Type;
	public int Index;
	public Vector3 Position;
}

[Tool]
public class CreatureSim : Simulation<DataCreature>
{
	public IReproductionStrategy ReproductionStrategy { get; private set; }

	public CreatureSim(SimulationWorld simulationWorld, bool useSexualReproduction = true) : base(simulationWorld)
	{
		ReproductionStrategy = useSexualReproduction ? new SexualReproductionStrategy() : new AsexualReproductionStrategy() as IReproductionStrategy;
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
		const float timeStep = 1f / SimulationWorld.PhysicsStepsPerSimSecond;
		
		// Process creatures. Doing one creature at a time for now with one big struct.
		// But eventually, it might make sense to do several loops which each work with narrower sets of data
		// For cache locality.
		// Idea for transitioning: Move all properties to lists in the DataEnitityRegistry. This will probably require
		// creating subclasses for different sims. Once that's done, we can still have a DataCreature class, but instead
		// of storing the data, DataCreature will serve as an intermediary to the registry with properties whose
		// getters and setters read from and write to the registry. This will ensure the system keeps working while
		// different parts of the code wean themselves off of the DataCreature idea.
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			
			if (!creature.Alive) continue;
			creature.Age += timeStep;
			if (creature.Age < CreatureSimSettings.MaturationTime)
			{
				Registry.Entities[i] = creature;
				continue;
			}
			if (creature.EatingTimeLeft > 0)
			{
				creature.EatingTimeLeft -= timeStep;
				Registry.Entities[i] = creature;
				continue;
			}

			// Food detection
			if (creature.Energy < creature.HungerThreshold)
			{
				var (closestFoodIndex, canEat) = CreatureSimSettings.FindClosestFood(creature);
				if (canEat && creature.EatingTimeLeft <= 0)
				{
					CreatureSimSettings.EatFood(ref creature, closestFoodIndex, i);
				}
				else if (closestFoodIndex > -1)
				{
					CreatureSimSettings.ChooseTreeDestination(ref creature, closestFoodIndex);
				}
			}
			
			// Reproduction
			if (creature.Energy > CreatureSimSettings.ReproductionEnergyThreshold)
			{
				var newCreature = ReproductionStrategy.Reproduce(ref creature, Registry);
				if (newCreature.Alive)
				{
					Registry.RegisterEntity(newCreature);
				}
			}

			// Move, updating destination if needed
			CreatureSimSettings.UpdatePositionAndVelocity(ref creature);
			var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			CreatureSimSettings.SpendMovementEnergy(ref creature);
			
			// Ded?
			CreatureSimSettings.CheckAndHandleCreatureDeath(ref creature, i);

			Registry.Entities[i] = creature;
		}
	}
}
