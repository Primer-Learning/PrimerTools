using System.Collections.Generic;
using Godot;
using EntityID = System.Int32;
namespace Aging.addons.PrimerTools.Simulation.Aging;

public class AgingSimEntityRegistry
{
    #region Entity Registry
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
			var transform = Transform3D.Identity.Translated(position).ScaledLocal(radius * Vector3.One);
			PhysicsServer3D.AreaSetTransform(area, transform);
			// Add a sphere collision shape to it
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
	
	#endregion
}