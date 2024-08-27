using System.Collections.Generic;
using Godot;
using System.Diagnostics;
using Aging.addons.PrimerTools.Simulation.Aging;
using Godot.Collections;
using PrimerTools;

[Tool]
public partial class AgingSim : Node3D
{
	#region Editor controls
	// [ExportGroup("Controls")]
	private bool _running;
	[Export]
	private bool Running
	{
		get => _running;
		set
		{
			if (value)
			{
				if (_stepsSoFar >= _maxNumSteps) Reset();
				if (_stepsSoFar == 0)
				{
					Initialize();
					GD.Print("Starting sim.");
				}
				else
				{
					GD.Print($"Continuing sim after step {_stepsSoFar}");
				}
			}
			else if (_running) // This is here so we only do this when stopping a running sim. Not when this gets called on build.
			{
				GD.Print($"Stopping sim after step {_stepsSoFar}");
				_stepsSoFar = 0;
				if (_stopwatch != null)
				{
					_stopwatch.Stop();
					GD.Print($"Elapsed time: {_stopwatch.Elapsed}");
				}
				Reset();
			}
			
			_running = value;
		}
	}
	private bool _resetUpButton = true;
	[Export]
	private bool ResetButton
	{
		get => _resetUpButton;
		set
		{
			if (!value && _resetUpButton && Engine.IsEditorHint())
			{
				Reset();
			}
			_resetUpButton = true;
		}
	}

	[Export] private bool _render;
	[Export] private bool _verbose;
	private Stopwatch _stopwatch;
	#endregion
	
	#region Sim parameters
	// [ExportGroup("Parameters")]
	private Rng _rng;
	[Export] private int _seed = -1;
	[Export] private int _initialCreatureCount = 4;
	[Export] private int _initialFoodCount = 100;
	[Export] private Vector2 _worldDimensions = Vector2.One * 10;
	[Export] private int _reproductionRatePer10K;
	[Export] private int _deathRatePer10K;
	[Export] private int _maxNumSteps = 100;
	[Export] private int _physicsStepsPerRealSecond = 60;
	private const int PhysicsStepsPerSimSecond = 60;
	private const float CreatureDestinationLength = 10f;
	private const float CreatureEatDistance = 0.5f;
	private const float FoodRegenerationTime = 1f;
	private const float EnergyGainFromFood = 1f;
	private const float ReproductionEnergyThreshold = 2f;
	private const float ReproductionEnergyCost = 1f;
	private const float MutationProbability = 0.1f;
	private const float MutationIncrement = 1f;
	private const float InitialCreatureSpeed = 20f;
	private const float InitialAwarenessRadius = 3f;
	private const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	private int _stepsSoFar = 0;
	#endregion
	
	public AgingSimEntityRegistry Registry = new();

	#region Simulation

	private void Initialize()
	{
		Registry.World3D = GetWorld3D();
		_stopwatch = Stopwatch.StartNew();
		
		_rng = new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
		PhysicsServer3D.SetActive(true);
		Engine.PhysicsTicksPerSecond = _physicsStepsPerRealSecond;

		for (var i = 0; i < _initialCreatureCount; i++)
		{
			var physicalCreature = Registry.CreateCreature(
				new Vector3(
					_rng.RangeFloat(_worldDimensions.X),
					0,
					_rng.RangeFloat(_worldDimensions.Y)
				),
				InitialAwarenessRadius,
				InitialCreatureSpeed,
				_render
			);
		}

		for (var i = 0; i < _initialFoodCount; i++)
		{
			Registry.CreateFood(
				new Vector3(
					_rng.RangeFloat(_worldDimensions.X),
					0,
					_rng.RangeFloat(_worldDimensions.Y)
				),
				_render
			);
		}
	}
	private bool Step()
	{
		if (Registry.PhysicalCreatures.Count == 0)
		{
			GD.Print("No Creatures found. Stopping.");
			Running = false;
			return false;
		}
		
		
		// Process creatures
		for (var i = 0; i < Registry.PhysicalCreatures.Count; i++)
		{
			var creature = Registry.PhysicalCreatures[i];
			if (!creature.Alive) continue;
			// Food detection
			var (closestFoodIndex, canEat) = FindClosestFood(creature);
			if (canEat)
			{
				EatFood(ref creature, closestFoodIndex);
			}
			else if (closestFoodIndex > -1)
			{
				ChooseDestination(ref creature, Registry.PhysicalFoods[closestFoodIndex]);
			}

			// Move
			GetNextPosition(ref creature);
			var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			
			// Reproduction and death
			SpendEnergy(ref creature);
			if (creature.Energy > ReproductionEnergyThreshold) Reproduce(ref creature);
			if (creature.Energy <= 0)
			{
				creature.Alive = false;
			}

			Registry.PhysicalCreatures[i] = creature;
		}
		RegenerateFood();
		
		if (!_verbose) return true;
		// Put debug stuff here. 
		return true;
	}
	public override void _PhysicsProcess(double delta)
	{
		// GD.Print("physics??");
		if (!_running) return;
		if (_stepsSoFar >= _maxNumSteps)
		{
			GD.Print("Done");
			Running = false;
			return;
		}
		
		if (Step()) _stepsSoFar++;
		if (!_verbose) return;
		if (_stepsSoFar % 100 == 0 ) GD.Print($"Finished step {_stepsSoFar}"); 
	}
	public override void _Process(double delta)
	{
		if (!_running) return;
		
		// Clean up the creature lists every process frame.
		// This is an intuitive choice for a frequency that isn't too high for sims with a fast physics loop
		// But high enough where things won't build up.
		// Could be a better choice, though.
		var deadIndices = new List<int>();
		for (var i = 0; i < Registry.PhysicalCreatures.Count; i++)
		{
			var physicalCreature = Registry.PhysicalCreatures[i];
			if (!physicalCreature.Alive)
			{
				deadIndices.Add(i);
				continue;
			}
			
			if (!_render) continue;
			var visualCreature = Registry.VisualCreatures[i];
			
			var transform = PhysicsServer3D.AreaGetTransform(physicalCreature.Body);
			// GD.Print(transform.Origin);
			RenderingServer.InstanceSetTransform(visualCreature.BodyMesh, transform);
			RenderingServer.InstanceSetTransform(visualCreature.AwarenessMesh, transform);
		}

		for (var i = deadIndices.Count - 1; i >= 0; i--)
		{
			var deadIndex = deadIndices[i];
			Registry.PhysicalCreatures[deadIndex].FreeRids();
			Registry.PhysicalCreatures.RemoveAt(deadIndex);
			
			if (!_render) continue;
			Registry.VisualCreatures[deadIndex].FreeRids();
			Registry.VisualCreatures.RemoveAt(deadIndex);
		}
	}

	private Array<Dictionary> DetectCollisionsWithCreature(AgingSimEntityRegistry.PhysicalCreature creature)
	{
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(creature.Awareness, 0);
		queryParams.Transform = Transform3D.Identity.Translated(creature.Position);

		// Run query and print
		return PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space).IntersectShape(queryParams);
	}
	#endregion

	#region Helpers

	private void GetNextPosition(ref AgingSimEntityRegistry.PhysicalCreature creature)
	{
		var stepSize = creature.Speed / PhysicsStepsPerSimSecond;
		if ((creature.CurrentDestination - creature.Position).LengthSquared() < stepSize * stepSize)
		{
			ChooseDestination(ref creature);
		}
		
		var displacement = (creature.CurrentDestination - creature.Position).Normalized() * stepSize;
		creature.Position += displacement;
	}

	private void ChooseDestination(ref AgingSimEntityRegistry.PhysicalCreature creature)
	{
		Vector3 newDestination;
		do
		{
			var angle = _rng.RangeFloat(1) * 2 * Mathf.Pi;
			var displacement = CreatureDestinationLength * new Vector3(
				Mathf.Sin(angle),
				0,
				Mathf.Cos(angle)
			);
			newDestination = creature.Position + displacement;
		} while (!IsWithinWorldBounds(newDestination));

		creature.CurrentDestination = newDestination;
	}
	
	private void ChooseDestination(ref AgingSimEntityRegistry.PhysicalCreature creature,
		AgingSimEntityRegistry.PhysicalFood food)
	{
		creature.CurrentDestination = food.Position;
	}

	private bool IsWithinWorldBounds(Vector3 position)
	{
		return position.X >= 0 && position.X <= _worldDimensions.X &&
		       position.Z >= 0 && position.Z <= _worldDimensions.Y;
	}
	
	private (int, bool) FindClosestFood(AgingSimEntityRegistry.PhysicalCreature creature)
	{
		var objectsInAwareness = DetectCollisionsWithCreature(creature);
		int closestFoodIndex = -1;
		var canEat = false;
		var closestFoodSqrDistance = float.MaxValue;

		foreach (var objectData in objectsInAwareness)
		{
			var objectIsFood = Registry.FoodLookup.TryGetValue((Rid)objectData["rid"], out var index);
			var food = Registry.PhysicalFoods[index];
			if (objectIsFood && !food.Eaten)
			{
				var sqrDistance = (creature.Position - food.Position).LengthSquared();
				if (!(sqrDistance < closestFoodSqrDistance)) continue;

				closestFoodSqrDistance = sqrDistance;
				closestFoodIndex = index;
				if (closestFoodSqrDistance < CreatureEatDistance * CreatureEatDistance)
				{
					canEat = true;
				}
			}
		}

		return (closestFoodIndex, canEat);
	}

	private void SpendEnergy(ref AgingSimEntityRegistry.PhysicalCreature creature)
	{
		var normalizedSpeed = creature.Speed / InitialCreatureSpeed;
		var normalizedAwarenessRadius = creature.AwarenessRadius / InitialAwarenessRadius;
		
		creature.Energy -= GlobalEnergySpendAdjustmentFactor * ( normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius) / PhysicsStepsPerSimSecond;
	}

	private void EatFood(ref AgingSimEntityRegistry.PhysicalCreature creature, int foodIndex)
	{
		var registryPhysicalFood = Registry.PhysicalFoods[foodIndex];
		registryPhysicalFood.Eaten = true;
		registryPhysicalFood.TimeLeftToRegenerate = FoodRegenerationTime;
		Registry.PhysicalFoods[foodIndex] = registryPhysicalFood;

		if (_render)
		{
			var foodBody = Registry.VisualFoods[foodIndex];
			RenderingServer.InstanceSetVisible(foodBody.BodyMesh, false);
		}

		creature.Energy += EnergyGainFromFood;
	}

	private void Reproduce(ref AgingSimEntityRegistry.PhysicalCreature creature)
	{
		var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
		
		float newAwarenessRadius = creature.AwarenessRadius;
		float newSpeed = creature.Speed;

		if (_rng.RangeFloat(0, 1) < MutationProbability)
		{
			newAwarenessRadius += _rng.RangeFloat(0, 1) < 0.5f ? MutationIncrement : -MutationIncrement;
			newAwarenessRadius = Mathf.Max(0, newAwarenessRadius);
		}

		if (_rng.RangeFloat(0, 1) < MutationProbability)
		{
			newSpeed += _rng.RangeFloat(0, 1) < 0.5f ? MutationIncrement : -MutationIncrement;
			newSpeed = Mathf.Max(0, newSpeed);
		}

		var physicalCreature = Registry.CreateCreature(
			transformNextFrame.Origin,
			newAwarenessRadius,
			newSpeed,
			_render
		);
		ChooseDestination(ref physicalCreature);
		creature.Energy -= ReproductionEnergyCost;
	}

	private void RegenerateFood()
	{
		for (var j = 0; j < Registry.PhysicalFoods.Count; j++)
		{
			var food = Registry.PhysicalFoods[j];
			if (food.Eaten)
			{
				food.TimeLeftToRegenerate -= 1f / PhysicsStepsPerSimSecond;
				if (food.TimeLeftToRegenerate <= 0)
				{
					RegenerateFood(ref food, j);
				}
				else
				{
					Registry.PhysicalFoods[j] = food;
				}
			}
		}
	}

	private void RegenerateFood(ref AgingSimEntityRegistry.PhysicalFood food, int index)
	{
		food.Eaten = false;
		food.TimeLeftToRegenerate = 0;
		Registry.PhysicalFoods[index] = food;

		if (_render)
		{
			var visualFood = Registry.VisualFoods[index];
			RenderingServer.InstanceSetVisible(visualFood.BodyMesh, true);
		}
	}

	#endregion

	private void Reset()
	{
		_stepsSoFar = 0;
		Registry.Reset();
	}
}
