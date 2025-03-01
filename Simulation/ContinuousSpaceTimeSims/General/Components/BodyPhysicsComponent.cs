using Godot;

namespace PrimerTools.Simulation;

public struct BodyPhysicsComponent : IComponent
{
    public EntityId EntityId { get; set; }
    public Transform3D Transform;
    private bool _kinematic;

    public Transform3D GetBodyTransform()
    {
        return Body.GetTransform3D();
    }

    public bool Kinematic
    {
        get => _kinematic;
        set
        {
            // Set the body mode
            if (value)
            {
                PhysicsServer3D.BodySetMode(Body.Body, PhysicsServer3D.BodyMode.Kinematic);
            }
            else
            {
                PhysicsServer3D.BodySetMode(Body.Body, PhysicsServer3D.BodyMode.Rigid);
            }
            _kinematic = value;
            // Perhaps some value handling will be needed
        }
    }

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
    public BodyComponent Body;
    public AreaComponent Awareness;

    public BodyPhysicsComponent(
        Rid space,
        Transform3D transform,
        Shape3D bodyShape,
        Transform3D bodyOffset = default,
        bool kinematic = true,
        float velocityDampingFactor = 0.99f,
        float angularVelocityDampingFactor = 0.99f
        ) : this()
    {
        Transform = transform;
        Velocity = Vector3.Zero;
        Body.Initialize(space, Transform, bodyOffset == default ? Transform3D.Identity : bodyOffset,
            bodyShape);

        Kinematic = kinematic;

        VelocityDampingFactor = velocityDampingFactor;
        AngularVelocityDampingFactor = angularVelocityDampingFactor;
    }

    public void AddCollisionException(BodyPhysicsComponent exception)
    {
        PhysicsServer3D.BodyAddCollisionException(Body.Body, exception.Body.Body);
    }
    public void RemoveCollisionException(BodyPhysicsComponent exception)
    {
        PhysicsServer3D.BodyRemoveCollisionException(Body.Body, exception.Body.Body);
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
        return Body.Body;
    }

    public void CleanUp()
    {
        Body.CleanUp();
        Awareness.CleanUp();
    }

    public void UpdateCollisionAreas()
    {
        if (Body.Body.IsValid)
        {
            Body.UpdateBaseTransform(Transform);
        }
        if (Awareness.Area.IsValid)
        {
            Awareness.UpdateTransform(Transform);
        }
    }
}
