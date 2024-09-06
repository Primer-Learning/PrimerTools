using Godot;

namespace PrimerTools.Simulation;

public struct VisualDebugCreature : IEntity
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

    public void Dispose()
    {
        FreeRids();
        BodyMeshResource?.Dispose();
        AwarenessMeshResource?.Dispose();
    }
}