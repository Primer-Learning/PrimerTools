using System;
using System.Linq;
using Godot;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public class CreatureSim : Simulation
{
	#region Simulation
	public DataEntityRegistry<DataCreature> Registry;
	private NodeCreatureManager _visualCreatureRegistry;
	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	private int _initialCreatureCount = 4;

	#region Life cycle
	public override void Initialize()
	{
		if (Initialized) return;
		// TODO: Not this. See comment in CreatureBehaviorHandler
		CreatureBehaviorHandler.FruitTreeSim = FruitTreeSim;
		CreatureBehaviorHandler.CreatureSim = this;
		CreatureBehaviorHandler.Space = PhysicsServer3D.SpaceGetDirectState(SimulationWorld.GetWorld3D().Space);
		
		Registry = new DataEntityRegistry<DataCreature>(SimulationWorld.World3D);
		
		switch (SimulationWorld.VisualizationMode)
		{
			case VisualizationMode.None:
				break;
			case VisualizationMode.NodeCreatures:
				SimulationWorld.GetChildren().OfType<NodeCreatureManager>().FirstOrDefault()?.Free();
				_visualCreatureRegistry = new NodeCreatureManager();
				SimulationWorld.AddChild(_visualCreatureRegistry);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		if (FruitTreeSim == null)
		{
			GD.PrintErr("TreeSim not found. Not initializing creature sim because they will all starve to death immediately. You monster.");
			return;
		}
		
		for (var i = 0; i < _initialCreatureCount; i++)
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

			RegisterCreature(physicalCreature);
		}

		StepsSoFar = 0;
		Initialized = true;
	}
	public override void Reset()
	{
		StepsSoFar = 0;
		Registry?.Reset();
		_visualCreatureRegistry?.Free();
		Initialized = false;
	}
	#endregion
	public override void Step()
	{
		if (!Running) return;
		if (Registry.Entities.Count == 0)
		{
			GD.Print("No Creatures found. Stopping.");
			Running = false;
			return;
		}

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
				_visualCreatureRegistry?.Entities[i].Eat(FruitTreeSim.VisualTreeRegistry.Entities[closestFoodIndex].GetFruit(), CreatureBehaviorHandler.EatDuration / SimulationWorld.TimeScale);
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
					RegisterCreature(CreatureBehaviorHandler.ReproduceAsexually(ref creature));
				}
				else
				{
					creature.OpenToMating = true;
					// Look for partner
					var (closestMateIndex, canMate) = CreatureBehaviorHandler.FindClosestPotentialMate(creature);
					if (canMate && creature.MatingTimeLeft <= 0)
					{
						RegisterCreature(CreatureBehaviorHandler.ReproduceSexually(ref creature, closestMateIndex));
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
				if (_visualCreatureRegistry != null)
				{
					var visualCreature = _visualCreatureRegistry.Entities[i];
					visualCreature.Death();
					_visualCreatureRegistry.Entities[i] = visualCreature;
				}
			}

			Registry.Entities[i] = creature;
		}
		
		StepsSoFar++;
	}

	public override void VisualProcess(double delta)
	{
		// Update visuals
		if (_visualCreatureRegistry != null)
		{
			for (var i = 0; i < Registry.Entities.Count; i++)
			{
				_visualCreatureRegistry.Entities[i].UpdateTransform(Registry.Entities[i]);
			}
		}
	}

	#endregion

	#region Registry interactions
	public override void ClearDeadEntities()
	{
		for (var i = Registry.Entities.Count - 1; i >= 0; i--)
		{
			if (Registry.Entities[i].Alive) continue;
			
			Registry.Entities[i].CleanUp();
			Registry.Entities.RemoveAt(i);

			if (_visualCreatureRegistry != null && _visualCreatureRegistry.Entities.Count > 0)
			{
				// Visual creatures aren't cleaned up here, since they may want to do an animation before freeing the object
				// But we clear the list here so they stay in sync.
				// For this reason, _creatureVisualizer.CreatureDeath must handle cleanup.
				_visualCreatureRegistry.RemoveCreature(i);
			}
		}
		
		// Rebuild TreeLookup
		Registry.EntityLookup.Clear();
		for (int i = 0; i < Registry.Entities.Count; i++)
		{
			Registry.EntityLookup[Registry.Entities[i].Body] = i;
		}
	}
	private void RegisterCreature(DataCreature dataCreature)
	{
		Registry.RegisterEntity(dataCreature);
		_visualCreatureRegistry?.RegisterEntity(dataCreature);
	}
	#endregion

	public CreatureSim(SimulationWorld simulationWorld) : base(simulationWorld)
	{
	}
}
