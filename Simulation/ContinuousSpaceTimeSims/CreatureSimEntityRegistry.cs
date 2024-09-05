using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class CreatureSimEntityRegistry
{
	public World3D World3D;
	
	// TODO: Make all physics and Debug visual objects with the server apis so we don't have to track C# wrappers.
	// Currently, we need to track them to stop the garbage collector from destroying them.
	public struct PhysicalCreature
	{
		public Rid Body;
		public Rid Awareness;
		
		public float AwarenessRadius;
		public float MaxSpeed;
		public bool Alive;
		public float Energy;
		public Vector3 Position;
		public Vector3 Velocity;
		public Vector3 CurrentDestination;
		public float EatingTimeLeft;
		
		public CapsuleShape3D BodyShapeResource;
		public SphereShape3D AwarenessShapeResource;
		
		public void FreeRids()
		{
			PhysicsServer3D.FreeRid(Body);
			PhysicsServer3D.FreeRid(Awareness);
		}
	}
	public struct VisualDebugCreature
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
	
	public readonly List<PhysicalCreature> PhysicalCreatures = new();
	public readonly List<VisualDebugCreature> VisualCreatures = new();
	public readonly List<Creature> NodeCreatures = new();
	
	// TODO: Maybe instead of passing visualization mode, we could pass the CreatureSim here, which would carry the 
	// visualization mode information and also provide a parent, preventing the need for this class to reference the 
	// CreatureSim.
	public PhysicalCreature CreateCreature(Vector3 position, float awarenessRadius, float speed, CreatureSim creatureSim)
	{
		var transform = Transform3D.Identity.Translated(position);
		
		// PhysicsServer3D stuff
		var bodyArea = PhysicsServer3D.AreaCreate();
		PhysicsServer3D.AreaSetSpace(bodyArea, World3D.Space);
		PhysicsServer3D.AreaSetTransform(bodyArea, transform);
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
			MaxSpeed = speed,
			Alive = true,
			BodyShapeResource = bodyShape,
			AwarenessShapeResource = awarenessShape,
			Position = position,
			Velocity = Vector3.Zero,
			CurrentDestination = position, // Will be changed immediately
			Energy = 1f
		};
		PhysicalCreatures.Add(physicalCreature);
		
		switch (creatureSim.VisualizationMode)
		{
			case VisualizationMode.None:
				break;
			case VisualizationMode.NodeCreatures:
				var creature = new Creature();
				creatureSim.AddChild(creature);
				creature.Name = "Creature"; 
				NodeCreatures.Add(creature);
				// creature.Owner = creatureSim.GetTree().EditedSceneRoot;
				break;
			case VisualizationMode.Debug:
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
					new VisualDebugCreature
					{
						BodyMesh = bodyMesh,
						AwarenessMesh = awarenessMesh,
						BodyMeshResource = bodyCapsule,
						AwarenessMeshResource = awarenessMeshResource
					}
				);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return physicalCreature;
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

	#endregion

	#region Cleanup
	public void Reset()
	{
		foreach (var creature in PhysicalCreatures) creature.FreeRids();
		foreach (var creature in VisualCreatures) creature.FreeRids();
		PhysicalCreatures.Clear();
		VisualCreatures.Clear();
		NodeCreatures.Clear();
	}

	public void ClearDeadCreatures()
	{
		for (var i = PhysicalCreatures.Count - 1; i >= 0; i--)
		{
			if (PhysicalCreatures[i].Alive) continue;
			
			PhysicalCreatures[i].FreeRids();
			PhysicalCreatures.RemoveAt(i);
			
			if (VisualCreatures.Count > 0)
			{
				VisualCreatures[i].FreeRids();
				VisualCreatures.RemoveAt(i);
			}
			if (NodeCreatures.Count > 0)
			{
				NodeCreatures[i].QueueFree();
				NodeCreatures.RemoveAt(i);
			}
		}
	}
	
	#endregion
}
