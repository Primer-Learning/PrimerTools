using Godot;
using PrimerTools.Simulation.TreeSim;

namespace PrimerTools.Simulation;

public struct VisualDebugTree : IVisualTree
{
    public Rid BodyMesh;
    public Rid FruitMesh;
    public CylinderMesh MeshResource;
    public SphereMesh FruitMeshResource;
    public Vector3 Position;
    public static World3D World3D { get; set; }
    
    private bool _fruitVisible;
    
    public void CleanUp()
    {
        RenderingServer.FreeRid(BodyMesh);
        if (FruitMesh != default)
        {
            RenderingServer.FreeRid(FruitMesh);
        }
    }

    public void UpdateTransform(PhysicalTree physicalTree)
    {
        var transform = Transform3D.Identity.Translated(physicalTree.Position);
        var scale = Vector3.One * Mathf.Min(1, physicalTree.Age / FruitTreeSim.TreeMaturationTime);
        transform = transform.ScaledLocal(Vector3.One * scale);
        RenderingServer.InstanceSetTransform(BodyMesh, transform);
    }
    
    public void Death()
    {
        RenderingServer.InstanceSetVisible(BodyMesh, false);
        if (FruitMesh != default)
        {
            RenderingServer.InstanceSetVisible(BodyMesh, false);
        }
    }

    public bool HasFruit => FruitMesh != default && _fruitVisible;
    public void GrowFruit(double duration)
    {
        if (_fruitVisible) return;

        if (FruitMesh == default)
        {
            var fruitMesh = new SphereMesh();
            fruitMesh.Radius = 0.6f;
            fruitMesh.Height = 1.2f;

            var fruitMaterial = new StandardMaterial3D();
            fruitMaterial.AlbedoColor = new Color(0, 1, 0); // Red color for the fruit
            fruitMesh.Material = fruitMaterial;

            var fruitInstance = RenderingServer.InstanceCreate2(fruitMesh.GetRid(), World3D.Scenario);
            var fruitTransform = Transform3D.Identity.Translated(Position + new Vector3(0, 1.5f, 0)); // Position the fruit above the tree
            RenderingServer.InstanceSetTransform(fruitInstance, fruitTransform);

            FruitMesh = fruitInstance;
            FruitMeshResource = fruitMesh;
        } 
        
        RenderingServer.InstanceSetVisible(FruitMesh, true);
        
        _fruitVisible = true;
    }

    public Node3D GetFruit()
    {
        // Only used when eating will be successful. So faking it.
        _fruitVisible = false;
        if (FruitMesh != default) RenderingServer.InstanceSetVisible(FruitMesh, false);
        return null;
    }

    public void Initialize(PhysicalTree physicalTree)
    {
        Position = physicalTree.Position;
        var transform = Transform3D.Identity.Translated(Position);
        var treeMesh = new CylinderMesh();
        treeMesh.TopRadius = 0.5f;
        treeMesh.BottomRadius = 0.5f;
        treeMesh.Height = 2.0f;
        MeshResource = treeMesh;

        var visualTransform = transform.ScaledLocal(0.5f * Vector3.One); // Smol because the original tree is not mature
        var bodyMesh = RenderingServer.InstanceCreate2(treeMesh.GetRid(), World3D.Scenario);
        BodyMesh = bodyMesh;
        RenderingServer.InstanceSetTransform(bodyMesh, visualTransform);
    }
}