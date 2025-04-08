using Godot;

namespace PrimerTools.Simulation;

public struct AreaComponent : IPhysicsObjectHandler
{
    public Rid Area { get; private set; }
    public Shape3D Shape { get; private set; }
    public Transform3D LocalTransform3D { get; private set; }
    
    public void Initialize(Rid space, Transform3D transform, Transform3D localTransform3D, Shape3D shape, uint collisionLayer = 1, uint collisionMask = 1)
    {
        var area = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(area, space);
        LocalTransform3D = localTransform3D;
        PhysicsServer3D.AreaSetTransform(area, transform * localTransform3D);
        PhysicsServer3D.AreaAddShape(area, shape.GetRid());
        PhysicsServer3D.AreaSetCollisionLayer(area, collisionLayer);
        PhysicsServer3D.AreaSetCollisionMask(area, collisionMask);

        Area = area;
        Shape = shape;
    }

    public void CleanUp()
    {
        Shape?.Dispose();
        if (Area == default) return; 
        PhysicsServer3D.FreeRid(Area);
    }

    public void UpdateTransform(Transform3D transform)
    {
        PhysicsServer3D.AreaSetTransform(Area, transform * LocalTransform3D);
    }

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

