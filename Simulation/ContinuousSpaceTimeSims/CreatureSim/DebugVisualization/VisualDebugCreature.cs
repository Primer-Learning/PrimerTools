using Godot;

namespace PrimerTools.Simulation;

public struct VisualDebugCreature : IVisualCreature
{
    public Rid BodyMesh;
    public Rid AwarenessMesh;
    public CapsuleMesh BodyMeshResource;
    public SphereMesh AwarenessMeshResource;
    public static World3D World3D;
	
    public void Initialize(PhysicalCreature physicalCreature)
    {
        var transform = Transform3D.Identity.Translated(physicalCreature.Position);
        
        // Body
        var bodyCapsule = new CapsuleMesh();
        bodyCapsule.Height = 1;
        bodyCapsule.Radius = 0.25f;
        BodyMeshResource = bodyCapsule;

        var bodyMesh = RenderingServer.InstanceCreate2(bodyCapsule.GetRid(), World3D.Scenario);
        BodyMesh = bodyMesh;
        RenderingServer.InstanceSetTransform(bodyMesh, transform);
        
        // Awareness
        var awarenessMeshResource = (SphereMesh)DefaultAwarenessBubbleMesh.Duplicate();
        awarenessMeshResource.Radius = physicalCreature.AwarenessRadius;
        awarenessMeshResource.Height = 2 * physicalCreature.AwarenessRadius;
        AwarenessMeshResource = awarenessMeshResource;
        
        var awarenessMesh = RenderingServer.InstanceCreate2(awarenessMeshResource.GetRid(), World3D.Scenario);
        AwarenessMesh = awarenessMesh;
        RenderingServer.InstanceSetTransform(awarenessMesh, transform);
    }

    public void CleanUp()
    {
        RenderingServer.FreeRid(BodyMesh);
        RenderingServer.FreeRid(AwarenessMesh);
        BodyMeshResource?.Dispose();
        AwarenessMeshResource?.Dispose();
    }

    public void Eat(Node3D food, float duration) {}
    public void Death() { CleanUp(); }
    public void UpdateTransform(PhysicalCreature physicalCreature)
    {
        var transform = Transform3D.Identity.Translated(physicalCreature.Position);
        RenderingServer.InstanceSetTransform(BodyMesh, transform);
        RenderingServer.InstanceSetTransform(AwarenessMesh, transform);
    }
    #region Object prep

    private static SphereMesh _cachedAwarenessBubbleMesh;
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
}