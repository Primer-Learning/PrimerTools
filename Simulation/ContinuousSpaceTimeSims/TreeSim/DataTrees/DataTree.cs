using Godot;

namespace PrimerTools.Simulation;

public struct DataTree : IEntity
{
    public Rid Body;
    public Vector3 Position;
    public float Age;
    public bool IsMature;
    public float TimeSinceLastSpawn;
    public SphereShape3D BodyShapeResource;
    public bool Alive;
    public bool HasFruit;
    public float FruitGrowthProgress;
 
    public static World3D World3D { get; set; }
    
    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Body);
    }

    public void Initialize()
    {
        var transform = Transform3D.Identity.Translated(Position);

        // PhysicsServer3D setup
        var bodyArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(bodyArea, World3D.Space);
        PhysicsServer3D.AreaSetTransform(bodyArea, transform);
        var bodyShape = new SphereShape3D();
        bodyShape.Radius = 1.0f;
        PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());
        
        Body = bodyArea;
        Age = 0;
        IsMature = false;
        TimeSinceLastSpawn = 0;
        BodyShapeResource = bodyShape;
        Alive = true;
    }
}