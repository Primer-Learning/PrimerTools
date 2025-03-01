using Godot;
using Godot.NativeInterop;

namespace PrimerTools.Simulation;

public class CharacterRigidBodyLiason
{
    public Transform3D LastTransform => Body.GetTransform3D();
    public Vector3 LastPosition => LastTransform.Origin;
    public Vector3 LastVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.LinearVelocity);
    public Vector3 LastAngularVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.AngularVelocity);
    
    public Vector3 NextAngularVelocity;
    
    public BodyComponent Body;

    public Vector3 NetForce;
    
    public CharacterRigidBodyLiason(
        Rid space,
        Transform3D transform,
        Shape3D bodyShape,
        Transform3D bodyOffset = default,
        float maxSpeed = 100000
        )
    {
        Body.Initialize(space, transform, bodyOffset == default ? Transform3D.Identity : bodyOffset,
            bodyShape);

        PhysicsServer3D.BodySetForceIntegrationCallback(
            Rid,
            Callable.From<PhysicsDirectBodyState3D, Variant>(IntegrateForces),
            maxSpeed
        );
    }

    private const float ForceMultiplier = 30;
    private void IntegrateForces(PhysicsDirectBodyState3D state, Variant userData)
    {
        var maxSpeed = (float)userData;
    
        // Calculate and apply the clamped force
        var finalForce = CalculateClampedForce(NetForce, state.LinearVelocity, maxSpeed, state.Step);
        state.ApplyForce(finalForce);
    
        state.AngularVelocity = NextAngularVelocity;
        
        NetForce = Vector3.Zero;
    }
    private Vector3 CalculateClampedForce(Vector3 netForce, Vector3 currentVelocity, float maxSpeed, float timeStep)
    {
        var currentSpeed = currentVelocity.Length();
    
        if (currentSpeed >= maxSpeed && netForce.Dot(currentVelocity) > 0)
        {
            return Vector3.Zero;
        }
    
        var rawForce = netForce * ForceMultiplier;
        var rawForceLength = rawForce.Length();
    
        if (rawForceLength < 0.0001f)
        {
            return Vector3.Zero;
        }
    
        var speedDelta = maxSpeed - currentSpeed;
        var maxForce = speedDelta / timeStep;
    
        if (rawForceLength > maxForce && maxForce > 0)
        {
            var scale = maxForce / rawForceLength;
            return rawForce * scale;
        }
    
        return rawForce;
    }
    public void AddCollisionException(CharacterRigidBodyLiason exception)
    {
        PhysicsServer3D.BodyAddCollisionException(Body.Body, exception.Body.Body);
    }
    public void RemoveCollisionException(CharacterRigidBodyLiason exception)
    {
        PhysicsServer3D.BodyRemoveCollisionException(Body.Body, exception.Body.Body);
    }

    public Rid Rid => Body.Body;

    public void CleanUp()
    {
        Body.CleanUp();
    }
}
