using Godot;

namespace PrimerTools.Simulation;

public struct DataTree : IDataEntity
{
    public Rid Body { get; set; }
    public Vector3 Position;
    public float Age;
    public bool IsMature => Age >= FruitTreeSimSettings.TreeMaturationTime;
    public float TimeSinceLastSpawn;
    public SphereShape3D BodyShapeResource;
    public bool Alive { get; set; }
    public bool HasFruit;
    public float FruitGrowthProgress;
    
    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Body);
    }

    public void Initialize(Rid space)
    {
        var transform = Transform3D.Identity.Translated(Position);

        // PhysicsServer3D setup
        var bodyArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetCollisionLayer(bodyArea, 1);
        PhysicsServer3D.AreaSetSpace(bodyArea,space);
        PhysicsServer3D.AreaSetTransform(bodyArea, transform);
        var bodyShape = new SphereShape3D();
        bodyShape.Radius = 1.0f;
        PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());
        
        Body = bodyArea;
        TimeSinceLastSpawn = 0;
        BodyShapeResource = bodyShape;
        Alive = true;
    }
}