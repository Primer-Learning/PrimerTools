using Godot;

namespace PrimerTools.Simulation;

public struct BodyHandler : IPhysicsObjectHandler
{
    public Rid Rid { get; private set; }
    public Shape3D Shape { get; private set; }
    public Transform3D LocalTransform3D { get; private set; }

    public BodyHandler(Rid space, Transform3D transform, Transform3D localTransform3D, Shape3D shape,
        uint collisionLayer = 1, uint collisionMask = 1)
    {
        Rid = PhysicsServer3D.BodyCreate();
        Shape = shape;
        PhysicsServer3D.BodySetSpace(Rid, space);
        LocalTransform3D = localTransform3D;
        PhysicsServer3D.BodySetState(Rid, PhysicsServer3D.BodyState.Transform, transform);
        PhysicsServer3D.BodyAddShape(Rid, shape.GetRid());
        PhysicsServer3D.BodySetShapeTransform(Rid, 0, LocalTransform3D);
        PhysicsServer3D.BodySetParam(Rid, PhysicsServer3D.BodyParameter.CenterOfMass, LocalTransform3D.Origin);
        PhysicsServer3D.BodySetCollisionLayer(Rid, collisionLayer);
        PhysicsServer3D.BodySetCollisionMask(Rid, collisionMask);
    }

    public void Initialize(Rid space, Transform3D transform, Transform3D localTransform3D, Shape3D shape, uint collisionLayer = 1, uint collisionMask = 1)
    {
        GD.Print("Consider using the constructor instead");
        var body = PhysicsServer3D.BodyCreate();
        PhysicsServer3D.BodySetSpace(body, space);
        LocalTransform3D = localTransform3D;
        PhysicsServer3D.BodySetState(body, PhysicsServer3D.BodyState.Transform, transform);
        PhysicsServer3D.BodyAddShape(body, shape.GetRid());
        PhysicsServer3D.BodySetShapeTransform(body, 0, LocalTransform3D);
        PhysicsServer3D.BodySetParam(body, PhysicsServer3D.BodyParameter.CenterOfMass, LocalTransform3D.Origin);
        PhysicsServer3D.BodySetCollisionLayer(body, collisionLayer);
        PhysicsServer3D.BodySetCollisionMask(body, collisionMask);

        Rid = body;
        Shape = shape;
    }

    public void CleanUp()
    {
        if (Shape != null && Shape.GetReferenceCount() == 1)
        {
            Shape.Dispose();
        } 
        if (Rid == default) return; 
        PhysicsServer3D.FreeRid(Rid);
    }

    public void UpdateBaseTransform(Transform3D transform)
    {
        PhysicsServer3D.BodySetState(Rid, PhysicsServer3D.BodyState.Transform, transform);
    }

    public Transform3D Transform =>(Transform3D)PhysicsServer3D.BodyGetState(Rid, PhysicsServer3D.BodyState.Transform);

    public Area3D ConstructDebugNode(Node3D parent)
    {
        var bodyArea = new Area3D();
        bodyArea.CollisionLayer = 0;
        bodyArea.CollisionMask = 0;
        parent.AddChild(bodyArea);
        var bodyShape = new CollisionShape3D();
        bodyArea.AddChild(bodyShape);
        bodyShape.Shape = Shape;
        bodyArea.Transform = LocalTransform3D;
        return bodyArea;
    }
}

