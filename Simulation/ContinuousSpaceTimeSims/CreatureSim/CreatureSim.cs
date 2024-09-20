using System.Linq;
using Godot;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public class CreatureSim : Simulation<DataCreature>
{
	public CreatureSim(SimulationWorld simulationWorld) : base(simulationWorld) {}

	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	protected override void CustomInitialize()
	{
		// TODO: Not this. See comment in CreatureBehaviorHandler
		DataCreatureBehaviorHandler.FruitTreeSim = FruitTreeSim;
		DataCreatureBehaviorHandler.CreatureSim = this;
		DataCreatureBehaviorHandler.Space = PhysicsServer3D.SpaceGetDirectState(SimulationWorld.GetWorld3D().Space);
		DataCreatureBehaviorHandler.SimulationWorld = SimulationWorld;
		
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
				AwarenessRadius = DataCreatureBehaviorHandler.InitialAwarenessRadius,
				MaxSpeed = DataCreatureBehaviorHandler.InitialCreatureSpeed
			};

			Registry.RegisterEntity(physicalCreature);
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
			if (creature.Age < DataCreatureBehaviorHandler.MaturationTime)
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
			var (closestFoodIndex, canEat) = DataCreatureBehaviorHandler.FindClosestFood(creature);
			if (canEat && creature.EatingTimeLeft <= 0)
			{
				DataCreatureBehaviorHandler.EatFood(ref creature, closestFoodIndex, i);
			}
			else if (closestFoodIndex > -1)
			{
				DataCreatureBehaviorHandler.ChooseTreeDestination(ref creature, closestFoodIndex);
			}
			
			// Reproduction
			if (creature.Energy > DataCreatureBehaviorHandler.ReproductionEnergyThreshold)
			{
				if (DataCreatureBehaviorHandler.CurrentSexMode == DataCreatureBehaviorHandler.SexMode.Asexual)
				{
					var newCreature = DataCreatureBehaviorHandler.ReproduceAsexually(ref creature);
					Registry.RegisterEntity(newCreature);
				}
				else
				{
					creature.OpenToMating = true;
					// Look for partner
					var (closestMateIndex, canMate) = DataCreatureBehaviorHandler.FindClosestPotentialMate(creature);
					if (canMate && creature.MatingTimeLeft <= 0)
					{
						var newCreature = DataCreatureBehaviorHandler.ReproduceSexually(ref creature, closestMateIndex); 
						Registry.RegisterEntity(newCreature);
					}
					else if (closestMateIndex > -1)
					{
						DataCreatureBehaviorHandler.ChooseMateDestination(ref creature, closestMateIndex);
					}
				}
			}

			// Move, updating destination if needed
			DataCreatureBehaviorHandler.UpdatePositionAndVelocity(ref creature);
			var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			DataCreatureBehaviorHandler.SpendMovementEnergy(ref creature);
			
			// Ded?
			if (creature.Energy <= 0)
			{
				creature.Alive = false;
				DataCreatureBehaviorHandler.HandleCreatureDeath(i);
			}

			Registry.Entities[i] = creature;
		}
	}
}
