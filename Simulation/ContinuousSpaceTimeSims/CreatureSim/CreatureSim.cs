using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public partial class CreatureSim : Node3D, ISimulation
{
    private Stopwatch _stepStopwatch = new Stopwatch();
    private Stopwatch _processStopwatch = new Stopwatch();
    private double _totalStepTime;
    private double _totalProcessTime;
    private int _stepCount;
    private int _processCount;
    [Signal] public delegate void SimulationInitializedEventHandler();
    [Export] private TreeSim _treeSim;
    private SimulationWorld SimulationWorld => GetParent<SimulationWorld>();

	#region Editor controls
	private bool _running;
	[Export]
	public bool Running
	{
		get => _running;
		set
		{
			if (value && _stepsSoFar == 0)
			{
				GD.Print("Starting sim creature sim");
				Initialize();
			}
			_running = value;
		}
	}

	#endregion
	
	#region Sim parameters
	[Export] private int _initialCreatureCount = 4;
	[Export] private float _initialCreatureSpeed = 5f;
	private const float CreatureStepMaxLength = 10f;
	private const float CreatureEatDistance = 1;
	private const float EnergyGainFromFood = 1f;
	private const float ReproductionEnergyThreshold = 2f;
	private const float ReproductionEnergyCost = 1f;
	private const float MutationProbability = 0.1f;
	private const float MutationIncrement = 1f;
	private const float InitialAwarenessRadius = 3f;
	private const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	private const float BaseEnergySpend = 0.1f;
	
	private const float MaxAccelerationFactor = 0.1f;
	private const float EatDuration = 0.5f;
	private int _stepsSoFar;
	#endregion
	
	public readonly CreatureSimEntityRegistry Registry = new();
	private ICreatureVisualizer _creatureVisualizer;

	#region Simulation

	private void Initialize()
	{
		Registry.World3D = SimulationWorld.World3D;
		switch (SimulationWorld.VisualizationMode)
		{
			case VisualizationMode.None:
				break;
			case VisualizationMode.Debug:
				_creatureVisualizer = new CreatureSimDebugVisualRegistry(SimulationWorld.World3D);
				break;
			case VisualizationMode.NodeCreatures:
				_creatureVisualizer = new CreatureSimNodeRegistry(this);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		GD.Print("Initializing");
		
		if (_treeSim == null)
		{
			GD.PrintErr("TreeSim not found. Not initializing creature sim because they will all starve to death immediately. You monster.");
			return;
		}
		
		for (var i = 0; i < _initialCreatureCount; i++)
		{
			var physicalCreature = new PhysicalCreature();
			physicalCreature.Position = new Vector3(
				SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
				0,
				SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
			);
			physicalCreature.AwarenessRadius = InitialAwarenessRadius;
			physicalCreature.MaxSpeed = _initialCreatureSpeed;
			
			Registry.RegisterEntity(physicalCreature);

			_creatureVisualizer.RegisterEntity(physicalCreature);
		}

		EmitSignal(SignalName.SimulationInitialized);
	}
	public void Step()
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
		
		// Process creatures. Doing one creature at a time for now with one big struct.
		// But eventually, it might make sense to do several loops which each work with narrower sets of data
		// For cache locality.
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			var creature = (PhysicalCreature)Registry.Entities[i];
			if (!creature.Alive) continue;

			// If eating, don't do anything else
			if (creature.EatingTimeLeft > 0)
			{
				creature.EatingTimeLeft -= 1f / SimulationWorld.PhysicsStepsPerSimSecond;
				Registry.Entities[i] = creature;
				continue;
			}

			// Food detection
			var (closestFoodIndex, canEat) = FindClosestFood(creature);
			if (canEat && creature.EatingTimeLeft <= 0)
			{
				EatFood(ref creature, closestFoodIndex);
				if (SimulationWorld.VisualizationMode == VisualizationMode.NodeCreatures)
				{
					((CreatureSimNodeRegistry)_creatureVisualizer).CreatureEat(i, _treeSim.Registry.NodeTrees[closestFoodIndex].GetFruit());
				}
			}
			else if (closestFoodIndex > -1)
			{
				ChooseDestination(ref creature, closestFoodIndex);
			}

			// Move, updating destination if needed
			UpdatePositionAndVelocity(ref creature);
			var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			
			// Reproduction and death
			SpendEnergy(ref creature);
			if (creature.Energy > ReproductionEnergyThreshold) Reproduce(ref creature);
			if (creature.Energy <= 0)
			{
				creature.Alive = false;
				if (SimulationWorld.VisualizationMode == VisualizationMode.NodeCreatures) ((Creature)((CreatureSimNodeRegistry)_creatureVisualizer).Entities[i]).Visible = false;
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
		for (var i = 0; i < Registry.Entities.Count; i++)
		{
			_creatureVisualizer.UpdateVisualCreature(i, Registry.Entities[i]);
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

	public void PrintPerformanceStats()
	{
		if (_stepCount > 0 && _processCount > 0)
		{
			GD.Print($"CreatureSim Performance Stats:");
			GD.Print($"  Average Step Time: {_totalStepTime / _stepCount:F3} ms");
			GD.Print($"  Average Process Time: {_totalProcessTime / _processCount:F3} ms");
		}
	}
	
	#region Behaviors
	private void UpdatePositionAndVelocity(ref PhysicalCreature creature)
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

	private void ChooseDestination(ref PhysicalCreature creature)
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
	
	private void ChooseDestination(ref PhysicalCreature creature, int treeIndex)
	{
		var tree = _treeSim.Registry.PhysicalTrees[treeIndex];
		creature.CurrentDestination = tree.Position;
	}
	
	private (int, bool) FindClosestFood(PhysicalCreature creature)
	{
		var objectsInAwareness = DetectCollisionsWithCreature(creature);
		var closestFoodIndex = -1;
		var canEat = false;
		var closestFoodSqrDistance = float.MaxValue;

		foreach (var objectData in objectsInAwareness)
		{
			var objectRid = (Rid)objectData["rid"];
			if (!_treeSim.Registry.TreeLookup.TryGetValue(objectRid, out var treeIndex)) continue;
			
			var tree = _treeSim.Registry.PhysicalTrees[treeIndex];
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

	private void SpendEnergy(ref PhysicalCreature creature)
	{
		var normalizedSpeed = creature.MaxSpeed / _initialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / InitialAwarenessRadius;
		
		creature.Energy -= (BaseEnergySpend + GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius)) / SimulationWorld.PhysicsStepsPerSimSecond;
	}

	private void EatFood(ref PhysicalCreature creature, int treeIndex)
	{
		var tree = _treeSim.Registry.PhysicalTrees[treeIndex];
		if (!tree.HasFruit) return;
		
		tree.HasFruit = false;
		tree.FruitGrowthProgress = 0;
		_treeSim.Registry.PhysicalTrees[treeIndex] = tree;
		
		creature.Energy += EnergyGainFromFood;
		creature.EatingTimeLeft = EatDuration;
	}

	private void Reproduce(ref PhysicalCreature creature)
	{
		creature.Energy -= ReproductionEnergyCost;

		var newCreature = creature;
		
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

		ChooseDestination(ref newCreature);
		Registry.RegisterEntity(newCreature);
		_creatureVisualizer.RegisterEntity(newCreature);
	}

	#endregion

	public void Reset()
	{
		_stepsSoFar = 0;
		Registry.Reset();
		_creatureVisualizer.Reset();
		
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
	}

	private void ClearDeadCreatures()
	{
		for (var i = Registry.Entities.Count - 1; i >= 0; i--)
		{
			if (((PhysicalCreature)Registry.Entities[i]).Alive) continue;
			
			Registry.Entities[i].Dispose();
			Registry.Entities.RemoveAt(i);

			if (_creatureVisualizer.Entities.Count > 0)
			{
				_creatureVisualizer.Entities[i].Dispose();
				_creatureVisualizer.Entities.RemoveAt(i);
			}
		}
	}
	
	#region Helpers
	private Array<Dictionary> DetectCollisionsWithCreature(PhysicalCreature creature)
	{
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(creature.Awareness, 0);
		queryParams.Transform = Transform3D.Identity.Translated(creature.Position);

		// Run query and print
		return PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space).IntersectShape(queryParams);
	}
	#endregion
}
