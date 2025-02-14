using System;
using Godot;

namespace PrimerTools.Simulation;


public struct BodyComponent
{
    public Rid Area { get; private set; }
    private Shape3D _shapeResource;
    
    public void Initialize(Rid space, Vector3 position, Shape3D shape)
    {
        var transform = Transform3D.Identity.Translated(position);
        
        var bodyArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(bodyArea, space);
        PhysicsServer3D.AreaSetTransform(bodyArea, transform);
        
        PhysicsServer3D.AreaAddShape(bodyArea, shape.GetRid());
        PhysicsServer3D.AreaSetCollisionLayer(bodyArea, 1);

        Area = bodyArea;
        _shapeResource = shape;
    }

    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Area);
        _shapeResource?.Dispose();
    }

    public void UpdateTransform(Transform3D transform)
    {
        PhysicsServer3D.AreaSetTransform(Area, transform);
    }
}

public struct AwarenessComponent
{
    public Rid Area { get; private set; }
    public float Radius { get; private set; }
    private SphereShape3D _awarenessShapeResource;
    
    public void Initialize(Rid space, Vector3 position, float radius)
    {
        Radius = radius;
        var transform = Transform3D.Identity.Translated(position);
        
        var awarenessArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(awarenessArea, space);
        PhysicsServer3D.AreaSetTransform(awarenessArea, transform);
        var awarenessShape = new SphereShape3D();
        awarenessShape.Radius = radius;
        PhysicsServer3D.AreaAddShape(awarenessArea, awarenessShape.GetRid());
        PhysicsServer3D.AreaSetCollisionLayer(awarenessArea, 2);
        PhysicsServer3D.AreaSetCollisionMask(awarenessArea, 1);

        Area = awarenessArea;
        _awarenessShapeResource = awarenessShape;
    }

    public void CleanUp()
    {
        PhysicsServer3D.FreeRid(Area);
        _awarenessShapeResource?.Dispose();
    }

    public void UpdateTransform(Transform3D transform)
    {
        PhysicsServer3D.AreaSetTransform(Area, transform);
    }
}

