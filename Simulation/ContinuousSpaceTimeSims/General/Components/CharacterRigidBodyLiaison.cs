using System.Linq;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

public class CharacterRigidBodyLiaison
{
    public Transform3D LastTransform => Body.Transform;
    public Vector3 LastPosition => LastTransform.Origin;
    public Vector3 LastVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.LinearVelocity);
    public Vector3 LastAngularVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.AngularVelocity);

    public BodyHandler Body;

    public Vector3 Destination;
    public Vector3 DesiredFacing;
    
    private float _mass;
    private float _maxSpeed;
    private float _maxAcceleration;
    private float _maxAngularVelocity = 5;
    private float _maxAngularAcceleration = 5;
    private bool _flying = false;

    private Rid _space;
    
    public CharacterRigidBodyLiaison(
        Rid space,
        Transform3D transform,
        Shape3D bodyShape,
        Transform3D bodyOffset = default,
        float maxSpeed = 100000,
        float maxAcceleration = 10,
        float mass = 50
        )
    {
        Body.Initialize(space, transform, bodyOffset == default ? Transform3D.Identity : bodyOffset,
            bodyShape);
        _maxSpeed = maxSpeed;
        _maxAcceleration = maxAcceleration;
        _mass = mass;
        _space = space;

        PhysicsServer3D.BodySetForceIntegrationCallback(
            Rid,
            Callable.From<PhysicsDirectBodyState3D>(IntegrateForces)
        );
        PhysicsServer3D.BodySetParam(Rid, PhysicsServer3D.BodyParameter.Mass, mass);
        
        if (_flying)
        {
            PhysicsServer3D.BodySetParam(Rid, PhysicsServer3D.BodyParameter.GravityScale, 0);
        }
        AddAxisLocks();
    }
    
    private Vector3 _netForce;
    private Vector3 _netTorque;

    public void AddExternalForce(Vector3 force)
    {
        _netForce += force;
    }
    public void AddExternalTorque(Vector3 torque)
    {
        _netTorque += torque;
    }
    
    public float TempSpeedMultiplier = 1;
    public void CalculateSelfInducedForces()
    {
        Vector3 surfaceNormal = Vector3.Zero;
        if (!_flying)
        {
            var raycastInfo = PhysicsServer3D.SpaceGetDirectState(_space).IntersectRay(
                new PhysicsRayQueryParameters3D()
                {
                    From = LastPosition + Vector3.Up * 0.05f,
                    To = LastPosition + Vector3.Down * 0.05f
                }
            );
            if (raycastInfo.Count > 0)
            {
                surfaceNormal = (Vector3)raycastInfo["normal"];
            }
            else return; // Can't apply forces on self if you're not standing on something (yet...)
        }
        
        var intendedDisplacement = Destination - LastPosition; 
        // Not certain whether the "intent" should be to move at max speed
        // The intent could be instantaneous movement, but in the end, it's limited.
        // Going with this for now. It at least naturally caps to max speed.
        // Could tweak or look up optimal control strategies if needed. Maybe dexterous characters use better algorithms? 
        var intendedVelocity = intendedDisplacement.Normalized() * _maxSpeed * TempSpeedMultiplier;
        var intendedAcceleration = (intendedVelocity - LastVelocity) * TempSpeedMultiplier;
        // Make intendedAcceleration parallel to the local plane
        // by projecting it onto the plane, then applying its original length
        // Not the most realistic adjustment to a hill, but it prevents getting stuck on a lip
        if (!_flying)
        {
            intendedAcceleration = (intendedAcceleration - intendedAcceleration.Dot(surfaceNormal) * surfaceNormal)
                                   .Normalized() * intendedAcceleration.Length();
        }
        
        // intendedAcceleration += intendedAcceleration.Dot(Vector3.Up)
        //     * (float)PhysicsServer3D.BodyGetParam(Rid, PhysicsServer3D.BodyParameter.GravityScale) * Vector3.Up;
        
        // The static friction coefficient appears to be 1.
        // I'm not certain if that's shape-independent.
        // But probably, since why else would a capsule turn out that way?
        var frictionFactor = _mass * 9.81f;
        var force = intendedAcceleration.LimitLength(_maxAcceleration * TempSpeedMultiplier) * _mass + intendedAcceleration.Normalized() * frictionFactor;
        if (force.LengthSquared() > 0.001f)
        {
            _netForce += force;    
        }
        
        // Torque calculation
        var currentBasis = LastTransform.Basis;
        var desiredBasis = Transform3DUtils.BasisFromForwardAndUp(DesiredFacing, Vector3.Up);
        var (axis, angle) = currentBasis.GetAxisAngleRotationTowardBasis(desiredBasis);
        
        const float massIndependentSpringConstant = 1; // m/s^2 per meter of displacement per mass
        const float dampingRatio = 0.2f;
        var damping = dampingRatio * 2 * Mathf.Sqrt(massIndependentSpringConstant); // m/s^2 per m/s per mass
        var mainTorque = axis * angle * massIndependentSpringConstant;
        var dampingTorque = -LastAngularVelocity * damping;
        
        var totalTorque = mainTorque + dampingTorque;
        if (totalTorque.LengthSquared() > 0.001f)
        {
            _netTorque += totalTorque.LimitLength(_maxAngularAcceleration) * _mass;
        }
    }
    private void IntegrateForces(PhysicsDirectBodyState3D state)
    {
        state.ApplyForce(_netForce);
        state.ApplyTorque(_netTorque);
        _netForce = Vector3.Zero;
        _netTorque = Vector3.Zero;
    }

    public void AddAxisLocks()
    {
        PhysicsServer3D.BodySetAxisLock(Rid, PhysicsServer3D.BodyAxis.AngularX, true);
        PhysicsServer3D.BodySetAxisLock(Rid, PhysicsServer3D.BodyAxis.AngularZ, true);
    }
    public void RemoveAxisLocks()
    {
        PhysicsServer3D.BodySetAxisLock(Rid, PhysicsServer3D.BodyAxis.AngularX, false);
        PhysicsServer3D.BodySetAxisLock(Rid, PhysicsServer3D.BodyAxis.AngularZ, false);
    }
    public void AddCollisionException(CharacterRigidBodyLiaison exception)
    {
        PhysicsServer3D.BodyAddCollisionException(Body.Rid, exception.Body.Rid);
    }
    public void RemoveCollisionException(CharacterRigidBodyLiaison exception)
    {
        PhysicsServer3D.BodyRemoveCollisionException(Body.Rid, exception.Body.Rid);
    }

    public Rid Rid => Body.Rid;

    public void CleanUp()
    {
        Body.CleanUp();
    }
}
