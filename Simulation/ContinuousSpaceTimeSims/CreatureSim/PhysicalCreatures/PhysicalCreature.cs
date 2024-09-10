using Godot;

namespace PrimerTools.Simulation;

public struct PhysicalCreature : IEntity
{
    public Rid Body;
    public Rid Awareness;
		
    public float AwarenessRadius;
    public float MaxSpeed;
    public bool Alive;
    public float Age;
    public float Energy;
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 CurrentDestination;
    public float EatingTimeLeft;
		
    private CapsuleShape3D _bodyShapeResource;
    private SphereShape3D _awarenessShapeResource;
    public static World3D World3D { get; set; }

    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Body);
        PhysicsServer3D.FreeRid(Awareness);
        _bodyShapeResource?.Dispose();
        _awarenessShapeResource?.Dispose();
    }

    public void Initialize()
    {
        var transform = Transform3D.Identity.Translated(Position);
		
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
        awarenessShape.Radius = AwarenessRadius;
        PhysicsServer3D.AreaAddShape(awarenessArea, awarenessShape.GetRid());

        Body = bodyArea;
        Awareness = awarenessArea;
        Alive = true;
        Age = 0;
        _bodyShapeResource = bodyShape;
        _awarenessShapeResource = awarenessShape;
        Velocity = Vector3.Zero;
        CurrentDestination = Position; // Will be changed immediately
        Energy = 1f;
    }
}