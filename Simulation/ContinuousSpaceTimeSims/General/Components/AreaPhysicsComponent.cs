using Godot;

namespace PrimerTools.Simulation;

public struct AreaPhysicsComponent : IComponent
{
    public EntityId EntityId { get; set; }
    public Transform3D Transform;

    public Vector3 Position
    {
        get => Transform.Origin;
        set => Transform = Transform.Translated(value - Transform.Origin);
    }
    public Quaternion Quaternion
    {
        get => Transform.Basis.GetRotationQuaternion();
        set => Transform = new Transform3D(new Basis(value), Transform.Origin);
    }

    public float VelocityDampingFactor;
    public float AngularVelocityDampingFactor;
    
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    public AreaComponent Body;
    public AreaComponent Awareness;

    public AreaPhysicsComponent(Rid space, Vector3 position, Shape3D bodyShape, Vector3 bodyOffset = default) 
        : this(space, Transform3D.Identity.Translated(position), bodyShape, bodyOffset) {}
    
    public AreaPhysicsComponent(
        Rid space,
        Transform3D transform,
        Shape3D bodyShape,
        Vector3 bodyOffset = default,
        float velocityDampingFactor = 0.99f,
        float angularVelocityDampingFactor = 0.99f
        ) : this()
    {
        Transform = transform;
        Velocity = Vector3.Zero;
        Body.Initialize(space, Transform, Transform3D.Identity.Translated(bodyOffset),
            bodyShape);

        VelocityDampingFactor = velocityDampingFactor;
        AngularVelocityDampingFactor = angularVelocityDampingFactor;
    }

    public void AddAwareness(Rid space, float awarenessRadius, Vector3 awarenessOffset =
        default)
    {
        Awareness.Initialize(
            space,
            Transform,
            Transform3D.Identity.Translated(awarenessOffset),
            new SphereShape3D { Radius = awarenessRadius }
        );
    }

    public Rid GetBodyRid()
    {
        return Body.Area;
    }

    public void CleanUp()
    {
        Body.CleanUp();
        Awareness.CleanUp();
    }

    public void UpdateCollisionAreas()
    {
        if (Body.Area != default)
        {
            Body.UpdateTransform(Transform);
        }
        if (Awareness.Area != default)
        {
            Awareness.UpdateTransform(Transform);
        }
    }
    
    public void AccelerateTowardTarget(Vector3 targetDestination, float maxSpeed, float accelerationFactor = 0.1f)
    {
        if (targetDestination == Vector3.Zero) GD.Print("Moving to the origin");
        var desiredDisplacement = targetDestination - Position;
        var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
        
        var desiredVelocity = Vector3.Zero;
        if (desiredDisplacementLengthSquared != 0)
        {
            desiredVelocity = desiredDisplacement * maxSpeed;
        }
        
        var velocityChange = desiredVelocity - Velocity;
        var velocityChangeLengthSquared = velocityChange.LengthSquared();

        var maxAccelerationMagnitudeSquared = maxSpeed * maxSpeed * accelerationFactor * accelerationFactor;
        Vector3 accelerationVector;
        if (velocityChangeLengthSquared > maxAccelerationMagnitudeSquared)
        {
            accelerationVector = Mathf.Sqrt(maxAccelerationMagnitudeSquared / velocityChangeLengthSquared) * velocityChange;
        }
        else
        {
            accelerationVector = velocityChange;
        }

        var newVelocity = Velocity + accelerationVector;
        
        var velocityLengthSquared = newVelocity.LengthSquared();
        var maxSpeedSquared = maxSpeed * maxSpeed;
        if (velocityLengthSquared > maxSpeedSquared)
        {
            newVelocity = maxSpeed / Mathf.Sqrt(velocityLengthSquared) * newVelocity;
        }

        Velocity = newVelocity;
    }

    public void SetAngularVelocityTowardBasis(Basis desiredBasis)
    {
        var currentRotation = Transform.Basis.GetRotationQuaternion();
        var targetRotation = desiredBasis.GetRotationQuaternion();
        var rotationDifference = currentRotation.Inverse() * targetRotation;
            
        // Convert to axis-angle representation
        var axis = rotationDifference.GetAxis().Normalized();
        var angle = rotationDifference.GetAngle();
        if (angle > Mathf.Pi)
        {
            angle = 2 * Mathf.Pi - angle;
            axis = -axis;
        }
        var rotationSpeedFactor = 10f;
        var maxSpeed = 10f;
        AngularVelocity = axis * Mathf.Min(angle * rotationSpeedFactor, maxSpeed);
    }
}
