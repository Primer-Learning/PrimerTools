using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using Aging.addons.PrimerTools.Simulation.Aging;
using Godot.Collections;
using PrimerTools;
using EntityID = System.Int32;

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
	[Export] private Vector2 _worldDimensions = Vector2.One * 10;
	[Export] private int _reproductionRatePer10K;
	[Export] private int _deathRatePer10K;
	[Export] private int _maxNumSteps = 100;
	[Export] private int _stepsPerSecond = 10;
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
		Engine.PhysicsTicksPerSecond = _stepsPerSecond;

		for (var i = 0; i < _initialBlobCount; i++)
		{
			Registry.CreateCreature(
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

		Registry.CreateFood(
			Vector3.Zero,
			_render
		);
	}

	private void Step()
	{
		if (Registry.PhysicalCreatures.Count == 0)
		{
			GD.Print("No Creatures found. Stopping.");
			Running = false;
			return;
		}
		
		// Process them one at a time. Eventually it may make sense to go in stages.
		foreach (var creature in Registry.PhysicalCreatures)
		{
		    // Do detections, then updates
		    var objectsInAwareness = DetectCollisionsWithArea(creature.Awareness);
		    // GD.Print(intersectionData.Count);
		    foreach (var objectData in objectsInAwareness)
		    {
			    // Find the EntityID of the intersecting object
				var intersectionRID = (Rid) objectData["rid"];

				var foundFood = Registry.FoodLookup.TryGetValue(intersectionRID, out AgingSimEntityRegistry.PhysicalFood food);
				
			    if (foundFood)
			    {
				    GD.Print("FOOD");
			    }
		    }
		    
			// Move
			var displacement = new Vector3(_rng.RangeFloat(-1, 1), 0, _rng.RangeFloat(-1, 1));
			// var displacement = Vector3.Zero;
			// var displacement = Vector3.Left;
			
			// This gets the position from the physics server
			var transform = PhysicsServer3D.AreaGetTransform(creature.Awareness).Translated(displacement);
			PhysicsServer3D.AreaSetTransform(creature.Body, transform);
			PhysicsServer3D.AreaSetTransform(creature.Awareness, transform);
			
		// 	// Check for baybies
		// 	if (_rng.rand.NextDouble() < (double)_reproductionRatePer10K / 10000)
		// 	{
		// 		newBlobs.Add(
		// 			Registry.CreateCreature(
		// 				transform.Origin,
		// 				((SphereShape3D)PhysicsServer3D.ShapeGetData(PhysicsServer3D.AreaGetShape(creature.Awareness, 0)))
		// 				.Radius, //2,
		// 				GetWorld3D(),
		// 				_render
		// 			)
		// 		);
		// 	}
		// 	// Check for ded
		// 	if (_rng.rand.NextDouble() < (double)_deathRatePer10K / 10000)
		// 	{
		// 		dedBlobs.Add(entityID);
		// 	}
		// }
		// _livingCreatureIDs.AddRange(newBlobs);
		// foreach (var blob in dedBlobs)
		// {
		// 	_livingCreatureIDs.Remove(blob);
		}
		
		if (!_verbose) return;
		// Old intersection detection testing, but can put other debug stuff here. 
		
		// GD.Print($"Intersections: {intersectionData.Count}");
		// foreach (var intersection in intersectionData)
		// {
		// 	GD.Print($"    {intersection["rid"]}, {intersection["shape"]}, {intersection["collider"]}, {intersection["collider_id"]}");
		// }
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
		
		Step();
		_stepsSoFar++;
	}

	public override void _Process(double delta)
	{
		// GD.Print("pros");
		if (!_running || !_render) return;
		// GD.Print("ess");

		for (var i = 0; i < Registry.PhysicalCreatures.Count; i++)
		{
			var transform = PhysicsServer3D.AreaGetTransform(Registry.PhysicalCreatures[i].Body);
			var visualCreature = Registry.VisualCreatures[i];
			RenderingServer.InstanceSetTransform(visualCreature.BodyMesh, transform);
			RenderingServer.InstanceSetTransform(visualCreature.AwarenessMesh, transform);
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

	private void Reset()
	{
		_stepsSoFar = 0;
		Registry.Reset();
	}
}