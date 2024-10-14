using System;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

[Flags]
public enum ActionFlags
{
    None = 0,
    Move = 1 << 0,
    Eat = 1 << 1,
    Reproduce = 1 << 2
}

public struct DataCreature : IDataEntity
{
    // Checked before processing
    public bool Alive { get; set; }
    
    // Updated in MovementAction
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
    
    public float AwarenessRadius;
    public float MaxSpeed;
    
    public float Age;
    public float MaxAge;
    public float Energy;
    public float HungerThreshold;
    public int FoodTargetIndex;
    public float EatingTimeLeft;
    public float MatingTimeLeft;

    public bool OpenToMating => Energy > CreatureSimSettings.ReproductionEnergyThreshold && MatingTimeLeft <= 0;
    
    // New field for action flags
    public ActionFlags Actions;
		
    private CapsuleShape3D _bodyShapeResource;
    private SphereShape3D _awarenessShapeResource;

    // PhysicsServer3D.SpaceGetDirectState
    private static PhysicsDirectSpaceState3D _space;

    /// <summary>
    /// Sets the static _space field if it hasn't been set yet. We assume all creatures are in the same space.
    /// </summary>
    /// <param name="world3D"></param>
    private static void SetSpace(World3D world3D)
    {
        if (_space != null) return;
        _space = PhysicsServer3D.SpaceGetDirectState(world3D.Space);
    }
    
    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Body);
        PhysicsServer3D.FreeRid(Awareness);
        _bodyShapeResource?.Dispose();
        _awarenessShapeResource?.Dispose();
    }

    public void Initialize(World3D world3D)
    {
        SetSpace(world3D);
        
        var transform = Transform3D.Identity.Translated(Position);
		
        // PhysicsServer3D stuff
        var bodyArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(bodyArea, world3D.Space);
        PhysicsServer3D.AreaSetTransform(bodyArea, transform);
        var bodyShape = new CapsuleShape3D();
        bodyShape.Height = 1;
        bodyShape.Radius = 0.25f;
        PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());
		
        var awarenessArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(awarenessArea, world3D.Space);
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
    
    public Array<Dictionary> DetectCollisionsWithCreature()
    {
        var queryParams = new PhysicsShapeQueryParameters3D();
        queryParams.CollideWithAreas = true;
        queryParams.CollideWithBodies = false;
        queryParams.Exclude = new Array<Rid>() { Body };
        queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(Awareness, 0);
        queryParams.Transform = Transform3D.Identity.Translated(Position);

        // Run query and print
        return _space.IntersectShape(queryParams);
    }
}

