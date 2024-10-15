using System;
using Godot;

namespace PrimerTools.Simulation;

public struct DataCreature : IDataEntity
{
    public bool Alive { get; set; }
    
    public Rid Body { get; set; }
    public Rid Awareness;
    public Vector3 Position;
    public Vector3 Velocity;

    private Vector3 _currentDestination;
    public Vector3 CurrentDestination
    {
        get => _currentDestination;
        set
        {
            _currentDestination = value;
            if (value == Vector3.Zero)
            {
                PrimerGD.PushWarningWithStackTrace("Current destination is the origin");
            } 
        }
    }
    
    public Genome Genome { get; set; }
    
    // Properties to easily access expressed trait values
    public float MaxSpeed => Genome.GetTrait<float>("MaxSpeed").ExpressedValue;
    public float AwarenessRadius => Genome.GetTrait<float>("AwarenessRadius").ExpressedValue;
    public float MaxAge => Genome.GetTrait<float>("MaxAge").ExpressedValue;
    
    public float Age;
    public float Energy;
    public float HungerThreshold;
    public int FoodTargetIndex;
    public float EatingTimeLeft;
    public float MatingTimeLeft;

    public bool OpenToMating => Energy > CreatureSimSettings.ReproductionEnergyThreshold && MatingTimeLeft <= 0;
		
    private CapsuleShape3D _bodyShapeResource;
    private SphereShape3D _awarenessShapeResource;
    
    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Body);
        PhysicsServer3D.FreeRid(Awareness);
        _bodyShapeResource?.Dispose();
        _awarenessShapeResource?.Dispose();
    }

    public void Initialize(Rid space)
    {
        var transform = Transform3D.Identity.Translated(Position);
		
        // PhysicsServer3D stuff
        var bodyArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(bodyArea, space);
        PhysicsServer3D.AreaSetTransform(bodyArea, transform);
        var bodyShape = new CapsuleShape3D();
        bodyShape.Height = 1;
        bodyShape.Radius = 0.25f;
        PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());
		
        var awarenessArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(awarenessArea, space);
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
        HungerThreshold = CreatureSimSettings.DefaultHungerThreshold;
        FoodTargetIndex = -1;
    }
}
