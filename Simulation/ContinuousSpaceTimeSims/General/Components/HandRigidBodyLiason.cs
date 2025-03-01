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
        if (HeldWeapon != null)
        {
            var weaponMass = (float)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid,
                PhysicsServer3D.BodyParameter.Mass);
            effectiveMass += weaponMass;
            gravityCompensationForce = Vector3.Up * 9.8f * weaponMass;

            var centerOfMass = (Vector3)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid, PhysicsServer3D.BodyParameter.CenterOfMass);
            var centerOfMassOffsetFromHand = HeldWeapon.LastTransform * centerOfMass - LastPosition;
            GD.Print($"Center of mass offset: {centerOfMassOffsetFromHand}");
            
            var weaponOffset = HeldWeapon.LastPosition - LastPosition;
            GD.Print($"Weapon offset: {weaponOffset}");
            
            var weaponInertia = (Vector3)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid,
                PhysicsServer3D.BodyParameter.Inertia);
            GD.Print($"Weapon Inertia: {weaponInertia}");
            // This is zero! Apparently
            
            gravityCompensationTorque = -centerOfMassOffsetFromHand.Cross(Vector3.Down * 9.8f * weaponMass * 10);
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
        var dampingTorque = -relativeAngularVelocity * AngularVelocityDampingFactor;
        
        var totalTorque = (mainTorque + dampingTorque) * effectiveMass + gravityCompensationTorque * TorqueTuningFactor;
        if (totalTorque.LengthSquared() > 0)
        {
            state.ApplyTorque(totalTorque.LimitLength(MaxTorque));
        }
    }
    // private void IntegrateForces(PhysicsDirectBodyState3D state, Variant userData)
    //  {
    //      // Get physical parameters
    //      var handMass = (float)PhysicsServer3D.BodyGetParam(Rid, PhysicsServer3D.BodyParameter.Mass);
    //
    //      // Calculate the total effective mass (hand + any held object)
    //      var effectiveMass = handMass;
    //      if (HeldWeapon != null)
    //      {
    //          var weaponMass = (float)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid,
    //  PhysicsServer3D.BodyParameter.Mass);
    //          effectiveMass += weaponMass;
    //      }
    //
    //      // Position control using Hooke's law (F = -kx)
    //      var currentPosition = LastPosition;
    //      var desiredPosition = _mainBody.LastTransform * IntendedRelativePosition;
    //      var displacement = desiredPosition - currentPosition;
    //
    //      // Spring constant based on mass (stiffer for heavier objects)
    //      // This is like a natural frequency of oscillation
    //      float springConstant = effectiveMass * 100.0f; // Natural frequency parameter
    //      var springForce = displacement * springConstant;
    //
    //      // Critical damping coefficient (2 * sqrt(m * k))
    //      // This provides optimal damping to prevent oscillation
    //      float dampingCoefficient = 2.0f * Mathf.Sqrt(effectiveMass * springConstant);
    //      var relativeVelocity = LastVelocity - _mainBody.LastVelocity;
    //      var dampingForce = -relativeVelocity * dampingCoefficient;
    //
    //      // Apply the combined forces
    //      state.ApplyForce(springForce + dampingForce);
    //
    //      // Rotation control using similar principles
    //      // Get the inertia tensor (approximation)
    //      // var inertia = PhysicsServer3D.BodyGetDirectState(Rid).InverseMass;
    //      var effectiveInertia = (float)PhysicsServer3D.BodyGetParam(Rid, PhysicsServer3D.BodyParameter.Mass); // Approximation of rotational inertia
    //
    //      // Calculate desired rotation
    //      var currentBasis = LastTransform.Basis;
    //      var desiredBasis = _mainBody.LastTransform.Basis * IntendedRelativeBasis;
    //
    //      // Get rotation axis and angle
    //      var (axis, angle) = currentBasis.GetAxisAngleRotationTowardBasis(desiredBasis);
    //
    //      // Apply torque proportional to angle and inertia
    //      var rotSpringConstant = effectiveInertia * 50.0f;
    //      var springTorque = axis * angle * rotSpringConstant;
    //
    //      // Critical damping for rotation
    //      float rotDampingCoefficient = 2.0f * Mathf.Sqrt(effectiveInertia * rotSpringConstant);
    //      var relativeAngularVelocity = LastAngularVelocity - _mainBody.LastAngularVelocity;
    //      var dampingTorque = -relativeAngularVelocity * rotDampingCoefficient;
    //
    //      // Apply the combined torques
    //      state.ApplyTorque(springTorque + dampingTorque);
    //
    //      // Gravity compensation for held weapon
    //      if (HeldWeapon != null)
    //      {
    //          var weaponMass = (float)PhysicsServer3D.BodyGetParam(HeldWeapon.Rid,
    //  PhysicsServer3D.BodyParameter.Mass);
    //          var gravityCompensation = Vector3.Up * 9.8f * weaponMass;
    //          state.ApplyForce(gravityCompensation);
    //
    //          // Compensate for the torque caused by the weapon's center of mass offset
    //          var weaponOffset = HeldWeapon.LastPosition - LastPosition;
    //          var leverTorque = weaponOffset.Cross(Vector3.Down * 9.8f * weaponMass);
    //          state.ApplyTorque(leverTorque);
    //      }
    //  }
    
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
