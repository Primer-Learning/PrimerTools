using Godot;
using System.Collections.Generic;
using SCG = System.Collections.Generic;
using System.Linq;
using Godot.Collections;
using GC = Godot.Collections;
using PrimerTools;
using EntityID = System.Int32;

[Tool]
public partial class AgingSim : Node3D
{
    [Export] private int _maxNumSteps = 6;
	private int _stepsSoFar = 0;
	[Export] private int _ticksPerSecond = 10;
	[Export] private bool _render;
	
	#region Sim parameters
	private Rng _rng;
	[Export] private int _seed = -1;
	[Export] private int _initialBlobCount = 4;
	[Export] private Vector2 _worldDimensions = Vector2.One * 10;
	#endregion
	
	#region Editor controls
	private bool _running;
	[Export]
	private bool Running
	{
		get => _running;
		set
		{
			_running = value;
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
			else
			{
				GD.Print($"Stopping sim after step {_stepsSoFar}");
				_stepsSoFar = 0;
				Reset();
			}
		}
	}
	private bool _cleanUpButton = true;
	[Export]
	private bool CleanupButton
	{
		get => _cleanUpButton;
		set
		{
			GD.Print("running");
			if (!value && _cleanUpButton && Engine.IsEditorHint())
			{
				Reset();
			}
			_cleanUpButton = true;
		}
	}

	[Export] private bool _verbose;
	#endregion
	
	#region Entity Registry

	public readonly EntityRegistry Registry = new();
	public class EntityRegistry
	{
		// Currently, this uses simple integers for EntityIDs.
		// And all entity properties are stored in lists, where the index of the
		// list item corresponds to the EntityID of the entity the property belongs to
		// This could be refactored away, since the RIDs exist in godot's servers.
		// But not currently sure whether that will make things faster or easier to understand.
		
		private EntityID _nextId;
		// Rids used for the entities in the physics and rendering systems
		// TODO: Separate these into their own lists. Rendering should be optional.
		public readonly List<(Rid area, Rid visualInstance)> Entities = new();
		// Rids not gettable from the main entity Rids. Only tracked for cleanup.
		// So far, just meshes.
		public readonly List<Rid> OtherRenderingRIDs = new();
		public readonly List<Vector3> Positions = new();
		public EntityID CreateBlob(Vector3 position, float radius, World3D world3D, bool render)
		{
			var id = _nextId++;
			Positions.Add(position);
			
			// Create the area for the blob's awareness and put it in the physics space
			var area = PhysicsServer3D.AreaCreate();
			// PhysicsServer3D.AreaSetMonitorable(area, true);
			PhysicsServer3D.AreaSetSpace(area, world3D.Space);
			// Haven't looked at all the ways to declare a transform. Could be something better?
			var transform = Transform3D.Identity.Translated(position);
			PhysicsServer3D.AreaSetTransform(area, transform);
			// Add a sphere collision shape to it
			var shape = PhysicsServer3D.SphereShapeCreate();
			PhysicsServer3D.ShapeSetData(shape, radius);
			PhysicsServer3D.AreaAddShape(area, shape); //, Transform3D.Identity.Translated(-boxSize / 2));

			Rid visInstance;
			// Mesh and visual instance 
			if (render)
			{
				var sphere = new SphereMesh();
				sphere.Radius = radius;
				sphere.Height = 2 * radius;
				OtherRenderingRIDs.Add(sphere.GetRid());
				
				visInstance = RenderingServer.InstanceCreate2(sphere.GetRid(), world3D.Scenario);
				RenderingServer.InstanceSetTransform(visInstance, Transform3D.Identity);
			}
			else
			{
				visInstance = default;
			}
			
			
			Entities.Add((area, visInstance));
			return id;
		}
	}
	#endregion

	#region Simulation

	private List<EntityID> _livingEntities;
	private void Initialize()
	{
		_rng = new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
		PhysicsServer3D.SetActive(true);
		Engine.PhysicsTicksPerSecond = _ticksPerSecond;
		var world = GetWorld3D();

		_livingEntities = new();
		for (var i = 0; i < _initialBlobCount; i++)
		{
			_livingEntities.Add(
				Registry.CreateBlob(
					new Vector3(
						_rng.RangeFloat(_worldDimensions.X),
						0,
						_rng.RangeFloat(_worldDimensions.Y)
					),
					0.5f,
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
		// Because the physics system does not immediately update

		for (var i = 0; i < _livingEntities.Count; i++)
		{
			var entity = Registry.Entities[i];
			var displacement = new Vector3(_rng.RangeFloat(-1, 1), 0, _rng.RangeFloat(-1, 1));
			var transform = PhysicsServer3D.AreaGetTransform(entity.area).Translated(displacement);
			PhysicsServer3D.AreaSetTransform(entity.area, transform);
			RenderingServer.InstanceSetTransform(entity.visualInstance, transform);	
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

	#region Cleanup
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
			RenderingServer.FreeRid(entity.visualInstance);
		}
		foreach (var rid in Registry.OtherRenderingRIDs)
		{
			RenderingServer.FreeRid(rid);
		}
		Registry.Entities.Clear();
		Registry.OtherRenderingRIDs.Clear();
		
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
		GD.Print($"Found and deleted {intersectionData.Count} area rids and their shapes.");
		
		// May be redundant
		// PhysicsServer3D.FreeRid(area);
		// PhysicsServer3D.FreeRid(PhysicsServer3D.AreaGetShape(area, 0));
	}
	#endregion
}