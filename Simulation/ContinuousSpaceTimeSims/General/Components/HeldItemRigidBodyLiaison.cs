using System.Numerics;
using Godot;
using Vector3 = Godot.Vector3;

namespace PrimerTools.Simulation;

public class HeldItemRigidBodyLiaison
{
    public Transform3D LastTransform => Body.GetTransform3D();
    public Vector3 LastPosition => LastTransform.Origin;
    public Vector3 LastVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.LinearVelocity);
    public Vector3 LastAngularVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.AngularVelocity);
    
    public BodyComponent Body;

    public Vector3 IntendedRelativePosition; 
    public CharacterRigidBodyLiaison MainBody;

    public Basis IntendedRelativeBasis;
    
    public HeldItemRigidBodyLiaison(
        Rid space,
        // Transform3D transform,
        Shape3D bodyShape,
        // CharacterRigidBodyLiaison body,
        // Vector3 intendedRelativePosition,
        Basis intendedRelativeBasis = default,
        Transform3D bodyOffset = default
        )
    {
        Body.Initialize(space, Transform3D.Identity, bodyOffset == default ? Transform3D.Identity : bodyOffset,
            bodyShape);

        // IntendedRelativePosition = intendedRelativePosition;
        if (intendedRelativeBasis == default)
        {
            intendedRelativeBasis = Basis.Identity;
        }

        IntendedRelativeBasis = intendedRelativeBasis;
        
        PhysicsServer3D.BodySetForceIntegrationCallback(
            Rid,
            Callable.From<PhysicsDirectBodyState3D>(IntegrateForces)
        );
    }

    public void SetTransform(Transform3D transform3D)
    {
        PhysicsServer3D.BodySetState(Rid, PhysicsServer3D.BodyState.Transform, transform3D);
    }

    public void Detach()
    {
        MainBody = null;
        PhysicsServer3D.BodySetParam(Rid, PhysicsServer3D.BodyParameter.GravityScale, 1);
    }

    public float MaxForce;
    private const float MaxTorque = 1000;
    
    private Vector3 _netAcceleration;
    private Vector3 _netAngularAcceleration;

    public void CalculateAccelerations()
    {
        if (MainBody == null) return;
        var acceleration = DampedSpringAcceleration();
        if (acceleration.LengthSquared() > 0)
        {
            _netAcceleration += acceleration;
        }
        
        var angularAcceleration = DampedSpringAngularAcceleration();
        if (angularAcceleration.LengthSquared() > 0)
        {
            _netAngularAcceleration += angularAcceleration;
        }

        #region Affect the body

        // // Calculate equal and opposite forces/torques for the main body
        // var handToBodyVector = _mainBody.LastPosition - LastPosition;
        //
        // // Decompose force into parallel and perpendicular components
        // var parallelDirection = handToBodyVector.Normalized();
        // var parallelComponent = totalForce.Dot(parallelDirection) * parallelDirection;
        //
        // // Calculate force to apply to main body
        // var bodyForceFromHandForce = -parallelComponent;
        // var bodyForceFromHandTorque = Vector3.Zero;
        // if (totalTorque.LengthSquared() > 0)
        // {
        //     bodyForceFromHandTorque = handToBodyVector.Cross(totalTorque).Normalized() * totalTorque.Length();
        // }

        // var forceRatio = 1;
        // // Apply forces to body
        // if ((bodyForceFromHandForce + bodyForceFromHandTorque).LengthSquared() > 0)
        // {
        //    _mainBody.AddExternalForce((bodyForceFromHandForce + bodyForceFromHandTorque) * forceRatio);
        // }

        // Wait until everything else seems okay
        // Apply torque to body
        // var perpendicularComponent = totalForce - parallelComponent;
        // var bodyTorque = -handToBodyVector.Cross(perpendicularComponent);
        // if ((bodyTorque-totalTorque).LengthSquared() > 0)
        // {
        //     _mainBody.AddExternalTorque(bodyTorque - totalTorque);
        // }

        #endregion
    }
    
    private void IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (MainBody == null) return;
        state.ApplyForce((_netAcceleration / state.InverseMass).LimitLength(MaxForce), state.CenterOfMass);
        _netAcceleration = Vector3.Zero;
        // GD.Print(state.InverseInertiaTensor.Inverse());
        state.ApplyTorque((_netAngularAcceleration * state.InverseInertiaTensor.Inverse()).LimitLength(MaxTorque));
        _netAngularAcceleration = Vector3.Zero;
    }

    private Vector3 DampedSpringAcceleration()
    {
        const float massIndependentSpringConstant = 350; // m/s^2 per meter of displacement per mass
        const float dampingRatio = 1f;
        var damping = dampingRatio * 2 * Mathf.Sqrt(massIndependentSpringConstant); // m/s^2 per m/s per mass

        // Linear force calculation
        var currentPosition = LastPosition;
        var desiredPosition = MainBody.LastTransform * IntendedRelativePosition;
        var diffToDesiredPosition = desiredPosition - currentPosition;
        var positionAcceleration = diffToDesiredPosition * massIndependentSpringConstant;
        
        // Velocity damping
        var relativeVelocity = LastVelocity - MainBody.LastVelocity;
        var dampingAcceleration = -relativeVelocity * damping;
        
        // Apply
        return positionAcceleration + dampingAcceleration;
    }

    private Vector3 DampedSpringAngularAcceleration()
    {
        // Correction
        const float massIndependentSpringConstant = 300; // radian/s^2 per radian of displacement per mass
        var currentBasis = LastTransform.Basis;
        var desiredBasis = MainBody.LastTransform.Basis * IntendedRelativeBasis;
        var (axis, angle) = currentBasis.GetAxisAngleRotationTowardBasis(desiredBasis);
        var mainAngularAcceleration = axis * angle * massIndependentSpringConstant;
        
        // Damping
        const float dampingRatio = 1f;
        var damping = dampingRatio * 2 * Mathf.Sqrt(massIndependentSpringConstant); // m/s^2 per m/s per mass
        var relativeAngularVelocity = LastAngularVelocity - MainBody.LastAngularVelocity;
        var dampedAngularAcceleration = -relativeAngularVelocity * damping;
        
        return mainAngularAcceleration + dampedAngularAcceleration; 
    }

    #region Other movement methods in case needs change
    // Claude made this. Just keeping it here as a reminder that there are other control methods.
    private Vector3 CalculateAccelerationWithPid()
    {
        var integralError = Vector3.Zero;
        const float kp = 350f;
        const float kd = 37.4f;
        const float ki = 15f;
        const float integralLimit = 1.0f;
        
        // Standard PD terms
        var currentPosition = LastPosition;
        var desiredPosition = MainBody.LastTransform * IntendedRelativePosition;
        var positionError = desiredPosition - currentPosition;
        var positionTerm = positionError * kp;
    
        // Velocity term
        var desiredVelocity = MainBody.LastVelocity;
        var velocityError = LastVelocity - desiredVelocity;
        var velocityTerm = -velocityError * kd;
    
        // Integral term - accumulates error over time
        integralError += positionError * SimulationWorld.TimeStep;
    
        // Limit integral buildup (anti-windup)
        if (integralError.Length() > integralLimit)
            integralError = integralError.Normalized() * integralLimit;
    
        var integralTerm = integralError * ki;
    
        return positionTerm + velocityTerm + integralTerm;
    }
    private void SetCappedVelocitiesTowardTransform(PhysicsDirectBodyState3D state)
    {
        const float positionCorrectionMaxSpeed = 1;
        const float rotationCorrectionMaxSpeed = 1f;
        // Velocity-setting
        var currentPosition = LastPosition;
        var desiredPosition = MainBody.LastTransform * IntendedRelativePosition;
        var diffToDesiredPosition = desiredPosition - currentPosition;
        var distance = diffToDesiredPosition.Length();
        // Interpret distance as speed in meters per step,
        // cap it to the max speed,
        // and convert to meters per second
        var speed = Mathf.Min(distance, positionCorrectionMaxSpeed) / state.Step;
        if (distance > 0)
        {
            state.LinearVelocity = diffToDesiredPosition / distance * speed;
        }
        
        // Angle
        var currentBasis = LastTransform.Basis;
        var desiredBasis = MainBody.LastTransform.Basis * IntendedRelativeBasis;
        var (axis, angle) = currentBasis.GetAxisAngleRotationTowardBasis(desiredBasis);
        // Interpret angle as rot speed in radians per step,
        // cap it to the max rot speed,
        // and convert to radians per second
        var rotSpeed = Mathf.Min(angle * 0.99f, rotationCorrectionMaxSpeed) / state.Step;
        
        if ((axis * rotSpeed).LengthSquared() > 0)
        {
            // I don't get why this rotation is needed. Everything should already be in global space.
            // But it works with this in place, and it rotates wildly for non-identity parent transforms.
            // if I just set AngularVelocity = axis * rotSpeed
            var bodyState = PhysicsServer3D.BodyGetDirectState(MainBody.Rid);
            state.AngularVelocity = bodyState.Transform.Basis * axis * rotSpeed;
        }
    }
    #endregion
    
    public void AddCollisionException(CharacterRigidBodyLiaison exception)
    {
        PhysicsServer3D.BodyAddCollisionException(Body.Body, exception.Body.Body);
    }
    public void RemoveCollisionException(CharacterRigidBodyLiaison exception)
    {
        PhysicsServer3D.BodyRemoveCollisionException(Body.Body, exception.Body.Body);
    }

    public Rid Rid => Body.Body;

    public void CleanUp()
    {
        Body.CleanUp();
    }
}
