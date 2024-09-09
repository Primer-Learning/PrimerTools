using System;
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
		
    public CapsuleShape3D BodyShapeResource;
    public SphereShape3D AwarenessShapeResource;
    
    public void CleanUp()
    {
        FreeRids();
        BodyShapeResource?.Dispose();
        AwarenessShapeResource?.Dispose();
    }
    public void FreeRids()
    {
        PhysicsServer3D.FreeRid(Body);
        PhysicsServer3D.FreeRid(Awareness);
    }
}