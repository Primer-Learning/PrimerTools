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
    public Vector3 CurrentDestination;
    
    public Genome Genome { get; set; }
    
    // Properties to easily access expressed trait values
    public float MaxSpeed => Genome.GetTrait<float>("MaxSpeed").ExpressedValue;
    public float AdjustedSpeed
    {
        get
        {
            var antagonisticPleiotropy = Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
            var factor = 1f;
            if (antagonisticPleiotropy is { ExpressedValue: true }) factor = 2f;
            return MaxSpeed * factor;
        }
    }
        
    public float AwarenessRadius => Genome.GetTrait<float>("AwarenessRadius").ExpressedValue;
    
    public float Age;
    public bool ForcedMature;
    public float Energy;
    public float HungerThreshold;
    public int FoodTargetIndex;
    public float EatingTimeLeft;
    public float MatingTimeLeft;

    public bool OpenToMating
    {
        get
        {
            if (Energy < CreatureSimSettings.Instance.ReproductionEnergyThreshold) return false;
            if (MatingTimeLeft > 0) return false;
            var maxReproductionAge = Genome.GetTrait<float>("MaxReproductionAge");
            if (maxReproductionAge != null && Age > maxReproductionAge.ExpressedValue) return false;
            
            return true;
        }
    }
    
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
        PhysicsServer3D.AreaSetCollisionLayer(bodyArea, 1);
		
        var awarenessArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(awarenessArea, space);
        PhysicsServer3D.AreaSetTransform(awarenessArea, transform);
        var awarenessShape = new SphereShape3D();
        awarenessShape.Radius = AwarenessRadius;
        PhysicsServer3D.AreaAddShape(awarenessArea, awarenessShape.GetRid());
        PhysicsServer3D.AreaSetCollisionLayer(awarenessArea, 2);
        PhysicsServer3D.AreaSetCollisionMask(awarenessArea, 1);

        Body = bodyArea;
        Awareness = awarenessArea;
        Alive = true;
        _bodyShapeResource = bodyShape;
        _awarenessShapeResource = awarenessShape;
        Velocity = Vector3.Zero;
        CurrentDestination = Position; // Will be changed immediately
        Energy = 1f;
        HungerThreshold = CreatureSimSettings.Instance.HungerThreshold;
        FoodTargetIndex = -1;
    }
}
