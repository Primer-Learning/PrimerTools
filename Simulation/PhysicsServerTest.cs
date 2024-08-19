using Godot;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

[Tool]
public partial class PhysicsServerTest : Node3D
{
	[Export] private int _maxNumSteps = 6;
	private int _stepsSoFar = 0;
	
	private readonly List<Rid> _areaRIDs = new();
	private readonly List<Rid> _shapeRIDs = new();
	private readonly List<Rid> _meshRIDs = new();
	private readonly List<Rid> _visInstanceIDs = new();
	
	private readonly System.Collections.Generic.Dictionary<Rid, Rid> _areaVisualInstancePairs = new (); 
	
	#region Running toggle
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
			}
		}
	}
	private bool _hardCleanUpButton = true;
	[Export]
	private bool HardCleanupButton
	{
		get => _hardCleanUpButton;
		set
		{
			GD.Print("running");
			if (!value && _hardCleanUpButton && Engine.IsEditorHint())
			{
				Reset();
				HardCleanup();
			}
			_hardCleanUpButton = true;
		}
	}
	#endregion
	
	private void Initialize()
	{
		PhysicsServer3D.SetActive(true);
		Engine.PhysicsTicksPerSecond = 2;
		var space = GetWorld3D().Space;
		
		// Make two areas
		MakeBoxArea(space);
		MakeBoxArea(space);
	}
	
	private void Step()
	{
		GD.Print("Step");
		if (_areaRIDs.Count == 0)
		{
			GD.Print("No RIDs found to act on. Stopping.");
			Running = false;
			return;
		}
		
		var area = _areaRIDs[0];
		
		var intersectionData = DetectCollisionsWithArea(area);
		GD.Print($"Intersections: {intersectionData.Count}");
		foreach (var intersection in intersectionData)
		{
			GD.Print($"    {intersection["rid"]}, {intersection["shape"]}, {intersection["collider"]}, {intersection["collider_id"]}");
		}
		
		// If identity transform, move it to x = 10. Otherwise, move it to the origin. 
		PhysicsServer3D.AreaSetTransform(area,
			PhysicsServer3D.AreaGetTransform(area) == Transform3D.Identity
				? Transform3D.Identity.Translated(Vector3.Right * 10)
				: Transform3D.Identity);
		
		RenderingServer.InstanceSetTransform(_areaVisualInstancePairs[area],
			PhysicsServer3D.AreaGetTransform(area));	
	}
	public override void _PhysicsProcess(double delta)
	{
		// GD.Print("physics??");
		if (!_running) return;
		if (_stepsSoFar >= _maxNumSteps)
		{
			GD.Print("Done");
			Running = false;
			Reset();
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
	private Rid MakeBoxArea(Rid space)
	{
		return MakeBoxArea(space, Vector3.One);
	}
	private Rid MakeBoxArea(Rid space, Vector3 boxSize)
	{
		// Create the area and put it in the physics space
		var area = PhysicsServer3D.AreaCreate();
		_areaRIDs.Add(area);
		// PhysicsServer3D.AreaSetMonitorable(area, true);
		PhysicsServer3D.AreaSetSpace(area, space);
		
		// Give it a transform
		var transform = Transform3D.Identity;
		PhysicsServer3D.AreaSetTransform(area, transform);
		
		// Add a box collision shape to it
		var shape = PhysicsServer3D.BoxShapeCreate();
		_shapeRIDs.Add(shape);
		PhysicsServer3D.ShapeSetData(shape, boxSize);
		// var shapeData = PhysicsServer3D.ShapeGetData(shape);
		// Add the shape to the area and center it.
		PhysicsServer3D.AreaAddShape(area, shape, Transform3D.Identity.Translated(-boxSize / 2));

		var box = new BoxMesh().GetRid();
		_meshRIDs.Add(box);
		var visInstance = RenderingServer.InstanceCreate2(box, GetWorld3D().Scenario);
		_visInstanceIDs.Add(visInstance);
		_areaVisualInstancePairs.Add(area, visInstance);
		RenderingServer.InstanceSetTransform(visInstance, Transform3D.Identity);
		
		GD.Print($"Made an area with RID {area} and shape RID {shape}");
		return area;
	}
	private void Reset()
	{
		_stepsSoFar = 0;
		
		GD.Print($"Freeing {_areaRIDs.Count} RIDs.");
		foreach (var rid in _areaRIDs.Concat(_shapeRIDs))
		{
			PhysicsServer3D.FreeRid(rid);
		}
		foreach (var rid in _meshRIDs.Concat(_visInstanceIDs))
		{
			RenderingServer.FreeRid(rid);
		}
		_areaRIDs.Clear();
		_shapeRIDs.Clear();
		_meshRIDs.Clear();
		_visInstanceIDs.Clear();
	}
	private void HardCleanup()
	{
		var world = GetWorld3D();
		var space = world.Space;
		
		// There might be an offset required here.
		var area = MakeBoxArea(space, new Vector3(int.MaxValue, int.MaxValue, int.MaxValue));
		
		var queryParams = new PhysicsShapeQueryParameters3D();
		queryParams.CollideWithAreas = true;
		queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(area, 0);
		queryParams.Transform = PhysicsServer3D.AreaGetTransform(area);

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
		
		PhysicsServer3D.FreeRid(area);
		_areaRIDs.Clear();
	}
	public override void _Ready()
	{
		PhysicsServer3D.SetActive(true);
		// CreateAreasAndQueryIntersectionsTest();
	}
}
