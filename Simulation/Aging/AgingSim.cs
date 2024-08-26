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
	[Export] private int _initialBlobCount = 4;
	[Export] private int _initialFoodCount = 100;
	[Export] private Vector2 _worldDimensions = Vector2.One * 10;
	[Export] private int _reproductionRatePer10K;
	[Export] private int _deathRatePer10K;
	[Export] private int _maxNumSteps = 100;
	[Export] private int _physicsStepsPerRealSecond = 60;
	private int _physicsStepsPerSimSecond = 60;
	private float _creatureSpeed = 20f;
	private float _creatureDestinationLength = 10f;
	private float _creatureEatDistance = 0.5f;
	private float _energyLossPerSecond = 0.1f;
	private int _stepsSoFar = 0;
	private float _foodRegenerationTime = 1f;
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

		for (var i = 0; i < _initialBlobCount; i++)
		{
			var physicalCreature = Registry.CreateCreature(
				// Vector3.Right * 20,
				// Random option
				new Vector3(
					_rng.RangeFloat(_worldDimensions.X),
					0,
					_rng.RangeFloat(_worldDimensions.Y)
				),
				3,
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
		
		// Process them one at a time. Eventually it may make sense to go in stages.
		for (var i = 0; i < Registry.PhysicalCreatures.Count; i++)
		{
			var creature = Registry.PhysicalCreatures[i];
			if (!creature.Alive) continue;
			var transformThisFrame = PhysicsServer3D.AreaGetTransform(creature.Body); 
			
		    // Do detections, then updates
		    
			// Food detection and decision making
			var objectsInAwareness = DetectCollisionsWithArea(creature.Awareness);
			int closestFoodIndex = -1;
			var canEat = false;
			var closestFoodSqrDistance = float.MaxValue;
		    foreach (var objectData in objectsInAwareness)
		    {
				var objectIsFood = Registry.FoodLookup.TryGetValue((Rid) objectData["rid"], out var index);
				var food = Registry.PhysicalFoods[index];
			    if (objectIsFood && !food.Eaten)
			    {
				    var sqrDistance = (transformThisFrame.Origin - PhysicsServer3D.AreaGetTransform(food.Body).Origin)
					    .LengthSquared();
				    if (!(sqrDistance < closestFoodSqrDistance)) continue;
				    
				    closestFoodSqrDistance = sqrDistance; 
				    closestFoodIndex = index;
				    if (closestFoodSqrDistance < _creatureEatDistance * _creatureEatDistance)
				    {
					    canEat = true;
				    }
			    }
		    }
		    
		    if (canEat)
		    {
			    var registryPhysicalFood = Registry.PhysicalFoods[closestFoodIndex];
			    registryPhysicalFood.Eaten = true;
			    registryPhysicalFood.TimeLeftToRegenerate = _foodRegenerationTime;
			    Registry.PhysicalFoods[closestFoodIndex] = registryPhysicalFood;
			    var foodBody = Registry.VisualFoods[closestFoodIndex];
			    
			    if (_render)
			    {
				    RenderingServer.InstanceSetVisible(foodBody.BodyMesh, false);
			    }
			    
			    // Increase creature's energy when eating
			    creature.Energy += 1f;
		    }
		    else if (closestFoodIndex > -1) { ChooseDestination(ref creature, Registry.PhysicalFoods[closestFoodIndex]); }
		    
			// Move
			var transformNextFrame = GetNextTransform(ref creature);
			PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
			
			// Decrease energy
			creature.Energy -= _energyLossPerSecond / _physicsStepsPerSimSecond;
			
			// Check for reproduction
			if (creature.Energy > 2f)
			{
				var physicalCreature = Registry.CreateCreature(
					transformNextFrame.Origin,
					creature.AwarenessRadius,
					_render
				);
				ChooseDestination(ref physicalCreature);
				creature.Energy -= 1f; // Parent loses energy when reproducing
			}
			
			// Check for death
			if (creature.Energy <= 0)
			{
				creature.Alive = false;
			}

			Registry.PhysicalCreatures[i] = creature;
		}
		
		// Food regeneration
		for (var j = 0; j < Registry.PhysicalFoods.Count; j++)
		{
			var food = Registry.PhysicalFoods[j];
			if (food.Eaten)
			{
				food.TimeLeftToRegenerate -= 1f / _physicsStepsPerSimSecond;
				if (food.TimeLeftToRegenerate <= 0)
				{
					food.Eaten = false;
					food.TimeLeftToRegenerate = 0;
					Registry.PhysicalFoods[j] = food;
						
					if (_render)
					{
						var visualFood = Registry.VisualFoods[j];
						RenderingServer.InstanceSetVisible(visualFood.BodyMesh, true);
					}
				}
				else
				{
					Registry.PhysicalFoods[j] = food;
				}
			}
		}
		
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

	private Array<Dictionary> DetectCollisionsWithArea(Rid area)
	{
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(area, 0);
		queryParams.Transform = PhysicsServer3D.AreaGetTransform(area);

		// Run query and print
		return PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space).IntersectShape(queryParams);
	}
	#endregion

	#region Behaviors

	private Transform3D GetNextTransform(ref AgingSimEntityRegistry.PhysicalCreature creature)
	{
		// Simple displacements. Old but keeping in case they are useful for testing.
		// var displacement = new Vector3(_rng.RangeFloat(-1, 1), 0, _rng.RangeFloat(-1, 1));
		// var displacement = Vector3.Zero;
		// var displacement = Vector3.Left;
		// return PhysicsServer3D.AreaGetTransform(creature.Awareness).Translated(displacement);
		// Destination
		var currentTransform = PhysicsServer3D.AreaGetTransform(creature.Body);
		var stepSize = _creatureSpeed / _physicsStepsPerSimSecond;
		if ((creature.CurrentDestination.Origin - currentTransform.Origin).LengthSquared() < stepSize * stepSize)
		{
			ChooseDestination(ref creature);
		}
		
		var displacement = (creature.CurrentDestination.Origin - currentTransform.Origin).Normalized() * stepSize;
		
		return currentTransform.Translated(displacement);
	}

	private void ChooseDestination(ref AgingSimEntityRegistry.PhysicalCreature creature)
	{
		var currentTransform = PhysicsServer3D.AreaGetTransform(creature.Body);
		Vector3 newDestination;
		do
		{
			var angle = _rng.RangeFloat(1) * 2 * Mathf.Pi;
			var displacement = _creatureDestinationLength * new Vector3(
				Mathf.Sin(angle),
				0,
				Mathf.Cos(angle)
			);
			newDestination = currentTransform.Origin + displacement;
		} while (!IsWithinWorldBounds(newDestination));

		creature.CurrentDestination = new Transform3D(Basis.Identity, newDestination);
	}

	private bool IsWithinWorldBounds(Vector3 position)
	{
		return position.X >= 0 && position.X <= _worldDimensions.X &&
			   position.Z >= 0 && position.Z <= _worldDimensions.Y;
	}

	private void ChooseDestination(ref AgingSimEntityRegistry.PhysicalCreature creature,
		AgingSimEntityRegistry.PhysicalFood food)
	{
		creature.CurrentDestination = PhysicsServer3D.AreaGetTransform(food.Body);
	}

	#endregion

	private void Reset()
	{
		_stepsSoFar = 0;
		Registry.Reset();
	}
}
