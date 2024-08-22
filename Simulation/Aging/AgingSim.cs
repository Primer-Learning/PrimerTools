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

	private List<EntityID> _livingCreatureIDs = new();
	private List<EntityID> _liveFoodIDs = new();
	private void Initialize()
	{
		_stopwatch = Stopwatch.StartNew();
		
		_rng = new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
		PhysicsServer3D.SetActive(true);
		Engine.PhysicsTicksPerSecond = _stepsPerSecond;
		var world = GetWorld3D();

		_livingCreatureIDs.Clear();
		for (var i = 0; i < _initialBlobCount; i++)
		{
			_livingCreatureIDs.Add(
				Registry.CreateCreature(
					Vector3.Right * 10,
					// Random option
					// new Vector3(
					// 	_rng.RangeFloat(_worldDimensions.X),
					// 	0,
					// 	_rng.RangeFloat(_worldDimensions.Y)
					// ),
					3,
					world,
					_render
				)
			);
		}
		_liveFoodIDs.Add(
			Registry.CreateFood(
				Vector3.Zero,
				world,
				_render
			)
		);
	}

	private void Step()
	{
		if (Registry.Entities.Count == 0)
		{
			GD.Print("No RIDs found to act on. Stopping.");
			Running = false;
			return;
		}

		var newBlobs = new List<EntityID>();
		var dedBlobs = new List<EntityID>();

		// Process them one at a time. Eventually it may make sense to go in stages.
		foreach (var entityID in _livingCreatureIDs)
		{
			var entity = Registry.Entities[entityID];
			
		    // Do detections, then updates
		    var intersectionData = DetectCollisionsWithArea(entity.area);
		    foreach (var intersection in intersectionData)
		    {
			    
			    // Find the EntityID of the intersecting object
				var intersectionRID = (Rid) intersection["rid"];
			    EntityID collisionEntityID = -1;
			    for (var i = 0; i < Registry.Entities.Count; i++)
			    {
				    if (Registry.Entities[i].area != intersectionRID) continue;
				    collisionEntityID = i;
				    break;
			    }
			    
			    if (collisionEntityID == -1) GD.PrintErr("Collided with something not in the entity registry");

			    if (_liveFoodIDs.Contains(collisionEntityID))
			    {
				    GD.Print("FOOD");
			    }
		    }
		    
			// GD.Print(entityID);
			// Move
			// var displacement = new Vector3(_rng.RangeFloat(-1, 1), 0, _rng.RangeFloat(-1, 1));
			var displacement = Vector3.Left;
			
			// This gets the position from the physics server
			var transform = PhysicsServer3D.AreaGetTransform(entity.area).Translated(displacement);
			PhysicsServer3D.AreaSetTransform(entity.area, transform);
			
			// Check for baybies
			if (_rng.rand.NextDouble() < (double)_reproductionRatePer10K / 10000)
			{
				newBlobs.Add(
					Registry.CreateCreature(
						transform.Origin,
						2,
						GetWorld3D(),
						_render
					)
				);
			}
			// Check for ded
			if (_rng.rand.NextDouble() < (double)_deathRatePer10K / 10000)
			{
				dedBlobs.Add(entityID);
			}
		}
		_livingCreatureIDs.AddRange(newBlobs);
		foreach (var blob in dedBlobs)
		{
			_livingCreatureIDs.Remove(blob);
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
		foreach (var entityID in _livingCreatureIDs)
		{
			var entity = Registry.Entities[entityID];
			var transform = PhysicsServer3D.AreaGetTransform(entity.area);
			RenderingServer.InstanceSetTransform(entity.mesh, transform);
			RenderingServer.InstanceSetTransform(entity.extraMesh, transform.ScaledLocal(entity.awarenessRange * Vector3.One));
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