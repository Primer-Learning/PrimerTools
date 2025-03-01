using Godot;
using Godot.NativeInterop;

namespace PrimerTools.Simulation;

public class HandRigidBodyLiason
{
    public Transform3D LastTransform => Body.GetTransform3D();
    public Vector3 LastPosition => LastTransform.Origin;
    public Vector3 LastVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.LinearVelocity);
    public Vector3 LastAngularVelocity => (Vector3)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.AngularVelocity);
    
    public BodyComponent Body;

    public Vector3 IntendedRelativePosition;
    private readonly CharacterRigidBodyLiason _mainBody;

    public Basis IntendedRelativeBasis;

    public RigidBodyLiason HeldWeapon;
    
    public HandRigidBodyLiason(
        Rid space,
        Transform3D transform,
        Shape3D bodyShape,
        CharacterRigidBodyLiason body,
        Vector3 intendedRelativePosition,
        Basis intendedRelativeBasis = default,
        Transform3D bodyOffset = default,
        float maxForce = 100000
        )
    {
        Body.Initialize(space, transform, bodyOffset == default ? Transform3D.Identity : bodyOffset,
            bodyShape);
        _mainBody = body;

        IntendedRelativePosition = intendedRelativePosition;
        if (intendedRelativeBasis == default)
        {
            intendedRelativeBasis = Basis.Identity;
        }

        IntendedRelativeBasis = intendedRelativeBasis;
        
        PhysicsServer3D.BodySetForceIntegrationCallback(
            Rid,
            Callable.From<PhysicsDirectBodyState3D, Variant>(IntegrateForces),
            maxForce
        );
    }

    private const float MaxForce = 10000;
    private const float ForceTuningFactor = 1000;
    private const float VelocityDampingFactor = 20f; // Damping to reduce oscillation
    
    private const float MaxTorque = 100000000;
    private const float TorqueTuningFactor = 1f;
    private const float AngularVelocityDampingFactor = 0.1f;
    private void IntegrateForces(PhysicsDirectBodyState3D state, Variant userData)
    {
        // TODO: If needed. Properly use inertia tensor and weapon center of mass
        
        // Calculate the total effective mass (hand + any held object)
        var handMass = (float)PhysicsServer3D.BodyGetParam(Rid, PhysicsServer3D.BodyParameter.Mass);
        var effectiveMass = handMass;
        var gravityCompensationForce = Vector3.Zero; // Hands have no gravity.
        var gravityCompensationTorque = Vector3.Zero;

        var printing = false;
        
        if (HeldWeapon != null)
        {
            var weaponMass = (float)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid,
                PhysicsServer3D.BodyParameter.Mass);
            effectiveMass += weaponMass;
            gravityCompensationForce = Vector3.Up * 9.8f * weaponMass;

            var centerOfMass = (Vector3)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid, PhysicsServer3D.BodyParameter.CenterOfMass);
            var centerOfMassOffsetFromHand = HeldWeapon.LastTransform * centerOfMass - LastPosition;

            printing = true;
            GD.Print();
            GD.Print($"HandMass: {handMass}, Weapon mass: {weaponMass}");
            GD.Print($"Center of mass offset: {centerOfMassOffsetFromHand}");
            
            // var weaponOffset = HeldWeapon.LastPosition - LastPosition;
            // GD.Print($"Weapon offset: {weaponOffset}");
            
            // var weaponInertia = (Vector3)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid,
            //     PhysicsServer3D.BodyParameter.Inertia);
            // var weaponInertia = state.CenterOfMass;
            // GD.Print($"Weapon Inertia: {weaponInertia}");
            // This is zero! Apparently
            
            
            gravityCompensationTorque = centerOfMassOffsetFromHand.Cross(Vector3.Up * 9.8f * weaponMass);
            GD.Print($"Gravity compensation: {gravityCompensationTorque}");
        }
        
        // Linear force calculation
        var currentPosition = LastPosition;
        var desiredPosition = _mainBody.LastTransform * IntendedRelativePosition;
        var diffToDesiredPosition = desiredPosition - currentPosition;
        var positionForce = diffToDesiredPosition * ForceTuningFactor;
        
        // Velocity damping
        var relativeVelocity = LastVelocity - _mainBody.LastVelocity;
        var dampingForce = -relativeVelocity * VelocityDampingFactor;
    
        // Apply
        var totalForce = (positionForce + dampingForce) * effectiveMass + gravityCompensationForce;
        if (totalForce.LengthSquared() > 0)
        {
            state.ApplyForce(totalForce.LimitLength(MaxForce));
        }
        
        // Torque calculation
        var currentBasis = LastTransform.Basis;
        var desiredBasis = _mainBody.LastTransform.Basis * IntendedRelativeBasis;
        var (axis, angle) = currentBasis.GetAxisAngleRotationTowardBasis(desiredBasis);
        var mainTorque = axis * angle * TorqueTuningFactor;
        
        // Angular velocity damping
        var relativeAngularVelocity = LastAngularVelocity - _mainBody.LastAngularVelocity;
        // GD.Print(LastAngularVelocity);
        var dampingTorque = -relativeAngularVelocity * AngularVelocityDampingFactor;
        
        var totalTorque = (mainTorque + dampingTorque) * effectiveMass + gravityCompensationTorque * TorqueTuningFactor;
        if (printing)
        {
            GD.Print($"Current basis: {currentBasis}");
            GD.Print($"Total torque: {totalTorque}");
        }
        if (totalTorque.LengthSquared() > 0)
        {
            state.ApplyTorque(totalTorque);
            // state.ApplyTorque(totalTorque.LimitLength(MaxTorque));
        }
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
