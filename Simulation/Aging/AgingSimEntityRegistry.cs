using System.Collections.Generic;
using Godot;
using EntityID = System.Int32;
namespace Aging.addons.PrimerTools.Simulation.Aging;

public class AgingSimEntityRegistry
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
	// public readonly List<Rid> OtherRenderingRIDs = new();
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
		var shape = PhysicsServer3D.SphereShapeCreate();
		PhysicsServer3D.ShapeSetData(shape, radius); // Radius is 
		PhysicsServer3D.AreaAddShape(area, shape);
		// OtherRenderingRIDs.Add(shape);
		
		Rid bodyMesh = default;
		Rid awarenessMesh = default;
		// Mesh and visual instance
		if (render)
		{
			// Body
			var bodyCapsule = new CapsuleMesh();
			bodyCapsule.Height = 1;
			bodyCapsule.Radius = 0.25f;

			bodyMesh = RenderingServer.InstanceCreate2(bodyCapsule.GetRid(), world3D.Scenario);
			RenderingServer.InstanceSetTransform(bodyMesh, transform);
			
			// Awareness
			// Just use the cached mesh if radius is one. Otherwise, duplicate and resize.
			Resource thisMesh;
			if (radius == 1) thisMesh = AwarenessBubbleMesh;
			else
			{
				thisMesh = AwarenessBubbleMesh.Duplicate();
				((SphereMesh)thisMesh).Radius = radius;
				((SphereMesh)thisMesh).Height = 2 * radius;
			}
			awarenessMesh = RenderingServer.InstanceCreate2(thisMesh.GetRid(), world3D.Scenario);
			RenderingServer.InstanceSetTransform(awarenessMesh, transform);
		}
		
		Entities.Add((area, bodyMesh, awarenessMesh, radius));
		return id;
	}

	public EntityID CreateFood(Vector3 position, World3D world3D, bool render)
	{
		var id = _nextId++;
		
		// Create the area for the blob's awareness and put it in the physics space
		var area = PhysicsServer3D.AreaCreate();
		// PhysicsServer3D.AreaSetMonitorable(area, true);
		PhysicsServer3D.AreaSetSpace(area, world3D.Space);
		var transform = Transform3D.Identity.Translated(position);
		PhysicsServer3D.AreaSetTransform(area, transform);
		// Add a sphere collision shape to it
		var shape = PhysicsServer3D.SphereShapeCreate();
		PhysicsServer3D.ShapeSetData(shape, 1);
		// OtherRenderingRIDs.Add(shape);
		PhysicsServer3D.AreaAddShape(area, shape);
		
		Rid renderMesh = default;
		// Mesh and visual instance
		if (render)
		{
			renderMesh = RenderingServer.InstanceCreate2(FoodMesh.GetRid(), world3D.Scenario);
			RenderingServer.InstanceSetTransform(renderMesh, transform);
		}
		
		Entities.Add((area, renderMesh, default, 0));
		return id;
	}
	
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
		// May need to check for default once blobs die, if it's faster to free the rids
		foreach (var entity in Entities)
		{
			for (var i = 0; i < PhysicsServer3D.AreaGetShapeCount(entity.area); i++)
			{
				PhysicsServer3D.FreeRid(PhysicsServer3D.AreaGetShape(entity.area, i));
			}
			PhysicsServer3D.FreeRid(entity.area);
			RenderingServer.FreeRid(entity.mesh);
			RenderingServer.FreeRid(entity.extraMesh);
		}
		// foreach (var rid in OtherRenderingRIDs)
		// {
		// 	RenderingServer.FreeRid(rid);
		// }

		Entities.Clear();
		// OtherRenderingRIDs.Clear();
		_nextId = 0;
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