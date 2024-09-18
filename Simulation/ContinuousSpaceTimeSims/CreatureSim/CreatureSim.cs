using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public partial class CreatureSim : Simulation
{
	#region Editor controls
	private bool _running;
	[Export]
	public bool Running
	{
		get => _running;
		set
		{
			if (value && !_initialized)
			{
				Initialize();
			}
			_running = value;
		}
	}
	#endregion

	#region Simulation
	private SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
	public DataCreatureRegistry Registry;
	private IEntityRegistry<NodeCreature> _visualCreatureRegistry;
	[Export] public FruitTreeSim _fruitTreeSim;
	private int _stepsSoFar;
	
	[Export]
	private int _initialCreatureCount = 4;

	#region Life cycle
	private bool _initialized;
	public override void Initialize()
	{
		// TODO: Not this. See comment in CreatureBehaviorHandler
		CreatureBehaviorHandler.FruitTreeSim = _fruitTreeSim;
		CreatureBehaviorHandler.CreatureSim = this;
		CreatureBehaviorHandler.Space = PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space);
		
		Registry = new DataCreatureRegistry(SimulationWorld.World3D);
		
		switch (SimulationWorld.VisualizationMode)
		{
			case VisualizationMode.None:
				break;
			case VisualizationMode.NodeCreatures:
				_visualCreatureRegistry = new NodeCreatureRegistry(this);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		if (_fruitTreeSim == null)
		{
			GD.PrintErr("TreeSim not found. Not initializing creature sim because they will all starve to death immediately. You monster.");
			return;
		}
		
		for (var i = 0; i < _initialCreatureCount; i++)
		{
			var physicalCreature = new DataCreature();
			physicalCreature.Position = new Vector3(
				SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
				0,
				SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
			);
			physicalCreature.AwarenessRadius = CreatureBehaviorHandler.InitialAwarenessRadius;
			physicalCreature.MaxSpeed = CreatureBehaviorHandler.InitialCreatureSpeed;
			
			RegisterCreature(physicalCreature);
		}

		_stepsSoFar = 0;
		_initialized = true;
	}
	public override void Reset()
	{
		_stepsSoFar = 0;
		Registry?.Reset();
		_visualCreatureRegistry?.Reset();
		_initialized = false;
		
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
	}
	#endregion
	public override void Step()
	{
		if (!_running) return;
		if (Registry.Entities.Count == 0)
		{
			GD.Print("No Creatures found. Stopping.");
			Running = false;
			return;
		}

		if (SimulationWorld.PerformanceTest)
		{
			_stepStopwatch.Restart();
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
				_visualCreatureRegistry?.Entities[i].Eat(_fruitTreeSim.VisualTreeRegistry.Entities[closestFoodIndex].GetFruit(), CreatureBehaviorHandler.EatDuration / SimulationWorld.TimeScale);
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
		
		_stepsSoFar++;

		if (SimulationWorld.PerformanceTest)
		{
			_stepStopwatch.Stop();
			_totalStepTime += _stepStopwatch.Elapsed.TotalMilliseconds;
			_stepCount++;
		}
	}
	public override void _Process(double delta)
	{
		if (!_running) return;

		if (SimulationWorld.PerformanceTest)
		{
			_processStopwatch.Restart();
		}
		
		// Update visuals
		if (_visualCreatureRegistry != null)
		{
			for (var i = 0; i < Registry.Entities.Count; i++)
			{
				_visualCreatureRegistry.Entities[i].UpdateTransform(Registry.Entities[i]);
			}
		}
		
		// This happens every process frame, which is an intuitive choice
		// for a frequency that isn't too high for sims with a fast physics loop.
		// But high enough where things won't build up.
		// Could be a better choice, though. Probably less often if anything.
		ClearDeadCreatures();

		if (SimulationWorld.PerformanceTest)
		{
			_processStopwatch.Stop();
			_totalProcessTime += _processStopwatch.Elapsed.TotalMilliseconds;
			_processCount++;
		}
	}

	#endregion

	#region Performance testing 
	private Stopwatch _stepStopwatch = new Stopwatch();
	private Stopwatch _processStopwatch = new Stopwatch();
	private double _totalStepTime;
	private double _totalProcessTime;
	private int _stepCount;
	private int _processCount;
	public void PrintPerformanceStats()
	{
		if (_stepCount > 0 && _processCount > 0)
		{
			GD.Print($"CreatureSim Performance Stats:");
			GD.Print($"  Average Step Time: {_totalStepTime / _stepCount:F3} ms");
			GD.Print($"  Average Process Time: {_totalProcessTime / _processCount:F3} ms");
		}
	}
	#endregion

	#region Registry interactions
	private void ClearDeadCreatures()
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
				_visualCreatureRegistry.Entities.RemoveAt(i);
			}
		}
		
		// Rebuild TreeLookup
		Registry.CreatureLookup.Clear();
		for (int i = 0; i < Registry.Entities.Count; i++)
		{
			Registry.CreatureLookup[Registry.Entities[i].Body] = i;
		}
	}
	private void RegisterCreature(DataCreature dataCreature)
	{
		Registry.RegisterEntity(dataCreature);
		_visualCreatureRegistry?.RegisterEntity(dataCreature);
	}
	#endregion
}
