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
}
