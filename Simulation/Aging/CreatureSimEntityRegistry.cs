using System.Collections.Generic;
using Godot;
namespace Aging.addons.PrimerTools.Simulation.Aging;

public class CreatureSimEntityRegistry
{
	public World3D World3D;
	
	// TODO: Make all objects with the server apis so we don't have to track C# wrappers.
	// Currently, we need to track them to stop the garbage collector from destroying them.
	public struct PhysicalCreature
	{
		public Rid Body;
		public Rid Awareness;
		
		public float AwarenessRadius;
		public float Speed;
		public bool Alive;
		public float Energy;
		public Vector3 Position;
		public Vector3 CurrentDestination;
		
		public CapsuleShape3D BodyShapeResource;
		public SphereShape3D AwarenessShapeResource;
		
		public void FreeRids()
		{
			PhysicsServer3D.FreeRid(Body);
			PhysicsServer3D.FreeRid(Awareness);
		}
	}
	public struct VisualCreature
	{
		public Rid BodyMesh;
		public Rid AwarenessMesh;
		public CapsuleMesh BodyMeshResource;
		public SphereMesh AwarenessMeshResource;
		
		public void FreeRids()
		{
			RenderingServer.FreeRid(BodyMesh);
			RenderingServer.FreeRid(AwarenessMesh);
		}
	}
	public struct PhysicalFood
	{
		public Rid Body;
		public Vector3 Position;
		public bool Eaten;
		public SphereShape3D BodyShapeResource;
		public float TimeLeftToRegenerate;

		public void FreeRids()
		{
			PhysicsServer3D.FreeRid(Body);
		}
	}
	public struct VisualFood
	{
		public Rid BodyMesh;
		public SphereMesh MeshResource;
		
		public void FreeRids()
		{
			RenderingServer.FreeRid(BodyMesh);
		}
	}
	
	public readonly List<PhysicalCreature> PhysicalCreatures = new();
	public readonly List<VisualCreature> VisualCreatures = new();
	public readonly List<PhysicalFood> PhysicalFoods = new();
	public readonly List<VisualFood> VisualFoods = new();

	public readonly Dictionary<Rid, int> FoodLookup = new();
	
	public PhysicalCreature CreateCreature(Vector3 position, float awarenessRadius, float speed, bool render)
	{
		var transform = Transform3D.Identity.Translated(position);
		
		// PhysicsServer3D stuff
		var bodyArea = PhysicsServer3D.AreaCreate();
		PhysicsServer3D.AreaSetSpace(bodyArea, World3D.Space);
		PhysicsServer3D.AreaSetTransform(bodyArea, transform);
		// var position = transform.Origin;
		var bodyShape = new CapsuleShape3D();
		bodyShape.Height = 1;
		bodyShape.Radius = 0.25f;
		PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());
		
		var awarenessArea = PhysicsServer3D.AreaCreate();
		PhysicsServer3D.AreaSetSpace(awarenessArea, World3D.Space);
		PhysicsServer3D.AreaSetTransform(awarenessArea, transform);
		var awarenessShape = new SphereShape3D();
		awarenessShape.Radius = awarenessRadius;
		PhysicsServer3D.AreaAddShape(awarenessArea, awarenessShape.GetRid());

		var physicalCreature = new PhysicalCreature
		{
			Body = bodyArea,
			Awareness = awarenessArea,
			AwarenessRadius = awarenessRadius,
			Speed = speed,
			Alive = true,
			BodyShapeResource = bodyShape,
			AwarenessShapeResource = awarenessShape,
			Position = position,
			CurrentDestination = position, // Will be changed immediately
			Energy = 1f
		};
		PhysicalCreatures.Add(physicalCreature);
		
		if (!render) return physicalCreature;
		// RenderingServer stuff
		// Body
		var bodyCapsule = new CapsuleMesh();
		bodyCapsule.Height = 1;
		bodyCapsule.Radius = 0.25f;

		var bodyMesh = RenderingServer.InstanceCreate2(bodyCapsule.GetRid(), World3D.Scenario);
		RenderingServer.InstanceSetTransform(bodyMesh, transform);
			
		// Awareness
		SphereMesh awarenessMeshResource;
		if (awarenessRadius == 1) awarenessMeshResource = DefaultAwarenessBubbleMesh;
		else
		{
			awarenessMeshResource = (SphereMesh)DefaultAwarenessBubbleMesh.Duplicate();
			awarenessMeshResource.Radius = awarenessRadius;
			awarenessMeshResource.Height = 2 * awarenessRadius;
		}
		
		var awarenessMesh = RenderingServer.InstanceCreate2(awarenessMeshResource.GetRid(), World3D.Scenario);
		RenderingServer.InstanceSetTransform(awarenessMesh, transform);
			
		VisualCreatures.Add(
			new VisualCreature
			{
				BodyMesh = bodyMesh,
				AwarenessMesh = awarenessMesh,
				BodyMeshResource = bodyCapsule,
				AwarenessMeshResource = awarenessMeshResource
			}
		);

		return physicalCreature;
	}

	public void CreateFood(Vector3 position, bool render)
	{
		// Create the area for the blob's awareness and put it in the physics space
		var body = PhysicsServer3D.AreaCreate();
		// PhysicsServer3D.AreaSetMonitorable(area, true);
		PhysicsServer3D.AreaSetSpace(body, World3D.Space);
		var transform = Transform3D.Identity.Translated(position);
		PhysicsServer3D.AreaSetTransform(body, transform);
		// Add a sphere collision shape to it
		var shape = new SphereShape3D();
		shape.Radius = 1;
		PhysicsServer3D.AreaAddShape(body, shape.GetRid());

		var newFood = new PhysicalFood
		{
			Body = body,
			Position = position,
			Eaten = false,
			BodyShapeResource = shape
		};
		FoodLookup.Add(body, PhysicalFoods.Count);
		PhysicalFoods.Add(newFood);
		
		// Mesh and visual instance
		if (!render) return;
		var meshResource = FoodMesh;
		var mesh = RenderingServer.InstanceCreate2(meshResource.GetRid(), World3D.Scenario);
		RenderingServer.InstanceSetTransform(mesh, transform);

		VisualFoods.Add(
			new VisualFood
			{
				BodyMesh = mesh,
				MeshResource = meshResource
			}
		);
	}
	
	#region Object prep

	private SphereMesh _cachedAwarenessBubbleMesh;
	private SphereMesh DefaultAwarenessBubbleMesh {
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
	
	private SphereMesh _cachedFoodMesh;
	private SphereMesh FoodMesh {
		get
		{
			if (_cachedFoodMesh != null) return _cachedFoodMesh;

			_cachedFoodMesh = new SphereMesh();
			var mat = new StandardMaterial3D();
			mat.AlbedoColor = new Color(0, 1, 0);

			_cachedFoodMesh.Material = mat;

			return _cachedFoodMesh;
			
			// These both also work, at least for getting a material to show up on a simple mesh.
			// RenderingServer.InstanceGeometrySetMaterialOverride(renderMesh, mat.GetRid());
			// RenderingServer.InstanceSetSurfaceOverrideMaterial(renderMesh, 0, mat.GetRid());
		}
	}

	#endregion

	#region Reset
	public void Reset()
	{
		GD.Print("Resetting");
		// Creatures
		foreach (var creature in PhysicalCreatures)
		{
			PhysicsServer3D.FreeRid(creature.Body);
			PhysicsServer3D.FreeRid(creature.Awareness);
		}
		foreach (var creature in VisualCreatures)
		{
			RenderingServer.FreeRid(creature.BodyMesh);	
			RenderingServer.FreeRid(creature.AwarenessMesh);
		}
		PhysicalCreatures.Clear();
		VisualCreatures.Clear();
		// Foods
		foreach (var food in PhysicalFoods)
		{
			PhysicsServer3D.FreeRid(food.Body);
		}
		foreach (var food in VisualFoods)
		{
			RenderingServer.FreeRid(food.BodyMesh);
		}
		PhysicalFoods.Clear();
		VisualFoods.Clear();
		FoodLookup.Clear();
	}
	// private void HardCleanup()
	// {
	// 	var space = GetWorld3D().Space;
	// 	var area = PhysicsServer3D.AreaCreate();
	// 	PhysicsServer3D.AreaSetSpace(area, space);
	// 	PhysicsServer3D.AreaSetTransform(area, Transform3D.Identity);
	// 	
	// 	// Add a box collision shape to it
	// 	var shape = PhysicsServer3D.BoxShapeCreate();
	// 	var boxSize = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
	// 	PhysicsServer3D.ShapeSetData(shape, boxSize);
	// 	PhysicsServer3D.AreaAddShape(area, shape, Transform3D.Identity.Translated(-boxSize / 2));
	// 	
	// 	var queryParams = new PhysicsShapeQueryParameters3D();
	// 	queryParams.CollideWithAreas = true;
	// 	queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(area, 0);
	// 	queryParams.Transform = PhysicsServer3D.AreaGetTransform(area);
	//
	// 	// Check for intersections with other areas
	// 	var intersectionData = PhysicsServer3D.SpaceGetDirectState(space).IntersectShape(queryParams, maxResults: 256);
	// 	
	// 	foreach (var intersection in intersectionData)
	// 	{
	// 		var areaRID = (Rid)intersection["rid"];
	// 		for (var i = 0; i < PhysicsServer3D.AreaGetShapeCount(areaRID); i++)
	// 		{
	// 			PhysicsServer3D.FreeRid(PhysicsServer3D.AreaGetShape(areaRID, i));
	// 		}
	// 		PhysicsServer3D.FreeRid(areaRID);
	// 	}
	// 	// The -1 is because it detects collisions with itself
	// 	GD.Print($"Found and deleted {intersectionData.Count - 1} area rids and their shapes.");
	// }
	#endregion
}
