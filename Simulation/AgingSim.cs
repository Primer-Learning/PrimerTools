using System;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using SCG = System.Collections.Generic;
using System.Linq;
using Godot.Collections;
using GC = Godot.Collections;
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
	
	#region Entity Registry

	public EntityRegistry Registry = new();
	public class EntityRegistry
	{
		// Currently, this uses simple integers for EntityIDs.
		// And all entity properties are stored in lists, where the index of the
		// list item corresponds to the EntityID of the entity the property belongs to
		// This could be refactored away, since the RIDs exist in godot's servers.
		// But not currently sure whether that will make things faster or easier to understand.
		
		private EntityID _nextId;

		// Rids used for the entities in the physics and rendering systems
		public readonly List<(Rid area, Rid mesh, Rid extraMesh, float awarenessRange)> Entities = new();
		// Rids not gettable from the main entity Rids. Only tracked for cleanup.
		// So far, just meshes.
		// ReSharper disable once InconsistentNaming
		public readonly List<Rid> OtherRenderingRIDs = new();
		public EntityID CreateCreature(Vector3 position, float radius, World3D world3D, bool render)
		{
			var id = _nextId++;
			
			// Create the area for the blob's awareness and put it in the physics space
			var area = PhysicsServer3D.AreaCreate();
			// PhysicsServer3D.AreaSetMonitorable(area, true);
			PhysicsServer3D.AreaSetSpace(area, world3D.Space);
			var transform = Transform3D.Identity.Translated(position);
			PhysicsServer3D.AreaSetTransform(area, transform);
			// Add a sphere collision shape to it
			
			// Awareness physics object
			var shape = PhysicsServer3D.SphereShapeCreate();
			PhysicsServer3D.ShapeSetData(shape, radius);
			PhysicsServer3D.AreaAddShape(area, shape);
			
			Rid bodyMesh = default;
			Rid awarenessMesh = default;
			// Mesh and visual instance
			if (render)
			{
				// Body
				var bodyCapsule = new CapsuleMesh();
				bodyCapsule.Height = 1;
				bodyCapsule.Radius = 0.25f;
				OtherRenderingRIDs.Add(bodyCapsule.GetRid());

				bodyMesh = RenderingServer.InstanceCreate2(bodyCapsule.GetRid(), world3D.Scenario);
				RenderingServer.InstanceSetTransform(bodyMesh, transform);
				
				// Awareness
				awarenessMesh = RenderingServer.InstanceCreate2(AwarenessBubbleMesh.GetRid(), world3D.Scenario);
				transform.ScaledLocal(Vector3.One * radius);
				RenderingServer.InstanceSetTransform(awarenessMesh, transform);
			}
			
			Entities.Add((area, bodyMesh, awarenessMesh, radius));
			return id;
		}

		// public EntityID CreateFood(Vector3 position, World3D world3D, bool render)
		// {
		// 	var id = _nextId++;
		// 	var radius = 0.5f;
		// 	
		// 	// Create the area for the blob's awareness and put it in the physics space
		// 	var area = PhysicsServer3D.AreaCreate();
		// 	// PhysicsServer3D.AreaSetMonitorable(area, true);
		// 	PhysicsServer3D.AreaSetSpace(area, world3D.Space);
		// 	var transform = Transform3D.Identity.Translated(position);
		// 	PhysicsServer3D.AreaSetTransform(area, transform);
		// 	// Add a sphere collision shape to it
		// 	var shape = PhysicsServer3D.SphereShapeCreate();
		// 	PhysicsServer3D.ShapeSetData(shape, radius);
		// 	PhysicsServer3D.AreaAddShape(area, shape); //, Transform3D.Identity.Translated(-boxSize / 2));
		//
		// 	Rid visInstance;
		// 	// Mesh and visual instance 
		// 	if (render)
		// 	{
		// 		var sphere = new SphereMesh();
		// 		sphere.Radius = radius;
		// 		sphere.Height = 2 * radius;
		// 		OtherRenderingRIDs.Add(sphere.GetRid());
		// 		
		// 		visInstance = RenderingServer.InstanceCreate2(sphere.GetRid(), world3D.Scenario);
		// 		RenderingServer.InstanceSetTransform(visInstance, Transform3D.Identity);
		// 	}
		// 	else
		// 	{
		// 		visInstance = default;
		// 	}
		// 	
		// 	
		// 	Entities.Add((area, visInstance));
		// 	return id;
		// }
		
		#region Object prep

		private SphereMesh _cachedAwarenessBubbleMesh;
		private SphereMesh AwarenessBubbleMesh {
			get
			{
				if (_cachedAwarenessBubbleMesh != null) return _cachedAwarenessBubbleMesh;

				_cachedAwarenessBubbleMesh = new SphereMesh();
				
				var mat = new StandardMaterial3D();
				mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				mat.AlbedoColor = new Color(1, 1, 1, 0.25f);

				_cachedAwarenessBubbleMesh.Material = mat;

				return _cachedAwarenessBubbleMesh;
			}
		}

		#endregion
	}
	#endregion

	#region Simulation

	private List<EntityID> _livingEntityIDs = new();
	private void Initialize()
	{
		_stopwatch = Stopwatch.StartNew();
		
		_rng = new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
		PhysicsServer3D.SetActive(true);
		Engine.PhysicsTicksPerSecond = _stepsPerSecond;
		var world = GetWorld3D();

		_livingEntityIDs.Clear();
		for (var i = 0; i < _initialBlobCount; i++)
		{
			_livingEntityIDs.Add(
				Registry.CreateCreature(
					new Vector3(
						_rng.RangeFloat(_worldDimensions.X),
						0,
						_rng.RangeFloat(_worldDimensions.Y)
					),
					2,
					world,
					_render
				)
			);
		}
	}

	private void Step()
	{
		if (Registry.Entities.Count == 0)
		{
			GD.Print("No RIDs found to act on. Stopping.");
			Running = false;
			return;
		}

		// Do detections, then updates
		var newBlobs = new List<EntityID>();
		var dedBlobs = new List<EntityID>();

		// Process them one at a time. Eventually it may make sense to go in stages.
		foreach (var entityID in _livingEntityIDs)
		{
			// GD.Print(entityID);
			var entity = Registry.Entities[entityID];
			// Move
			var displacement = new Vector3(_rng.RangeFloat(-1, 1), 0, _rng.RangeFloat(-1, 1));
			
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
		_livingEntityIDs.AddRange(newBlobs);
		foreach (var blob in dedBlobs)
		{
			_livingEntityIDs.Remove(blob);
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
		foreach (var entityID in _livingEntityIDs)
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

	#region Reset
	private void Reset()
	{
		_stepsSoFar = 0;
		// May need to check for default once blobs die, if it's faster to free the rids
		foreach (var entity in Registry.Entities)
		{
			for (var i = 0; i < PhysicsServer3D.AreaGetShapeCount(entity.area); i++)
			{
				PhysicsServer3D.FreeRid(PhysicsServer3D.AreaGetShape(entity.area, i));
			}
			PhysicsServer3D.FreeRid(entity.area);
			RenderingServer.FreeRid(entity.mesh);
			RenderingServer.FreeRid(entity.extraMesh);
		}
		foreach (var rid in Registry.OtherRenderingRIDs)
		{
			RenderingServer.FreeRid(rid);
		}

		Registry = new();
		// Registry.Entities.Clear();
		// Registry.OtherRenderingRIDs.Clear();
		
		HardCleanup();
	}
	private void HardCleanup()
	{
		var space = GetWorld3D().Space;
		var area = PhysicsServer3D.AreaCreate();
		PhysicsServer3D.AreaSetSpace(area, space);
		PhysicsServer3D.AreaSetTransform(area, Transform3D.Identity);
		
		// Add a box collision shape to it
		var shape = PhysicsServer3D.BoxShapeCreate();
		var boxSize = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
		PhysicsServer3D.ShapeSetData(shape, boxSize);
		PhysicsServer3D.AreaAddShape(area, shape, Transform3D.Identity.Translated(-boxSize / 2));
		
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(area, 0);
		queryParams.Transform = PhysicsServer3D.AreaGetTransform(area);

		// Check for intersections with other areas
		var intersectionData = PhysicsServer3D.SpaceGetDirectState(space).IntersectShape(queryParams, maxResults: 256);
		
		foreach (var intersection in intersectionData)
		{
			var areaRID = (Rid)intersection["rid"];
			for (var i = 0; i < PhysicsServer3D.AreaGetShapeCount(areaRID); i++)
			{
				PhysicsServer3D.FreeRid(PhysicsServer3D.AreaGetShape(areaRID, i));
			}
			PhysicsServer3D.FreeRid(areaRID);
		}
		// The -1 is because it detects collisions with itself
		GD.Print($"Found and deleted {intersectionData.Count - 1} area rids and their shapes.");
	}
	#endregion
}