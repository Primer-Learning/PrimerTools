using System.Linq;
using Godot;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public class CreatureSim : Simulation<DataCreature, NodeCreature>
{
	public CreatureSim(SimulationWorld simulationWorld) : base(simulationWorld) {}
	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	protected override void CustomInitialize()
	{
		// TODO: Not this. See comment in CreatureBehaviorHandler
		CreatureBehaviorHandler.FruitTreeSim = FruitTreeSim;
		CreatureBehaviorHandler.CreatureSim = this;
		CreatureBehaviorHandler.Space = PhysicsServer3D.SpaceGetDirectState(SimulationWorld.GetWorld3D().Space);
		
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
				AwarenessRadius = CreatureBehaviorHandler.InitialAwarenessRadius,
				MaxSpeed = CreatureBehaviorHandler.InitialCreatureSpeed
			};

			RegisterEntity(physicalCreature);
		}
	}
	protected override void CustomStep()
	{
		const float timeStep = 1f / SimulationWorld.PhysicsStepsPerSimSecond;
		
		// Process creatures. Doing one creature at a time for now with one big struct.
		// But eventually, it might make sense to do several loops which each work with narrower sets of data
		// For cache locality.
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = Registry.Entities[i];
			
			if (!creature.Alive) continue;
			creature.Age += timeStep;
			if (creature.Age < CreatureBehaviorHandler.MaturationTime)
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
			var (closestFoodIndex, canEat) = CreatureBehaviorHandler.FindClosestFood(creature);
			if (canEat && creature.EatingTimeLeft <= 0)
			{
				CreatureBehaviorHandler.EatFood(ref creature, closestFoodIndex);
				(VisualRegistry?.Entities[i] as NodeCreature)?.Eat((FruitTreeSim.VisualRegistry?.Entities[closestFoodIndex] as NodeTree)?.GetFruit(), CreatureBehaviorHandler.EatDuration / SimulationWorld.TimeScale);
			}
			else if (closestFoodIndex > -1)
			{
				CreatureBehaviorHandler.ChooseTreeDestination(ref creature, closestFoodIndex);
			}
			
			// Reproduction
			if (creature.Energy > CreatureBehaviorHandler.ReproductionEnergyThreshold)
			{
				
				if (CreatureBehaviorHandler.CurrentSexMode == CreatureBehaviorHandler.SexMode.Asexual)
				{
					RegisterEntity(CreatureBehaviorHandler.ReproduceAsexually(ref creature));
				}
				else
				{
					creature.OpenToMating = true;
					// Look for partner
					var (closestMateIndex, canMate) = CreatureBehaviorHandler.FindClosestPotentialMate(creature);
					if (canMate && creature.MatingTimeLeft <= 0)
					{
						RegisterEntity(CreatureBehaviorHandler.ReproduceSexually(ref creature, closestMateIndex));
						// _visualCreatureRegistry?.Entities[i].Eat(_fruitTreeSim.VisualTreeRegistry.Entities[closestFoodIndex].GetFruit(), CreatureBehaviorHandler.EatDuration / SimulationWorld.TimeScale);
					}
					else if (closestMateIndex > -1)
					{
						CreatureBehaviorHandler.ChooseMateDestination(ref creature, closestMateIndex);
					}
				}
			}

			// Move, updating destination if needed
			CreatureBehaviorHandler.UpdatePositionAndVelocity(ref creature);
			var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			CreatureBehaviorHandler.SpendMovementEnergy(ref creature);
			
			// Ded?
			if (creature.Energy <= 0)
			{
				creature.Alive = false;
				if (VisualRegistry != null)
				{
					var visualCreature = VisualRegistry.Entities[i] as NodeCreature;
					visualCreature?.Death();
					VisualRegistry.Entities[i] = visualCreature;
				}
			}

			Registry.Entities[i] = creature;
		}
	}
	public override void VisualProcess(double delta)
	{
		if (VisualRegistry != null)
		{
			for (var i = 0; i < Registry.Entities.Count; i++)
			{
				VisualRegistry.Entities[i].UpdateTransform(Registry.Entities[i]);
			}
		}
	}
}
