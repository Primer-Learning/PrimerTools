using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PrimerTools.Simulation;

public static class CollisionDetector
{
    // Comments on detection
    // You can set the transformation of the shape before you do the IntersectShape query
    // This means the shapes can be modified based on entity attributes.
    // Currently, trees just use a scale 1 sphere and scale it according to the check being done.
    // But creatures and combatants construct a non-unit sphere according to their awareness radius stat. 
    // This could be fine, but it might be cleaner to use a scale 1 sphere for all detectors and just scale that according
    // to relevant values. There could even be a single detector that gets reused over and over.
    // That seems weird, but idk why it wouldn't work.
    
    // Also, the transform parameter in PhysicsQueryParameters3D overrides the area's transform.
    // If you don't provide the parameter, it's overridden by Transform.Identity.
    // Which means it will be as if your object is at the origin.
    // So we provide one.
    
    public static List<TypedCollision> GetOverlappingEntities(
        Rid detectionArea,
        Transform3D transform,
        Rid space,
        params Rid[] exclusions)
    {
        var queryParams = new PhysicsShapeQueryParameters3D
        {
            CollideWithAreas = true,
            CollideWithBodies = false,
            Exclude = new Array<Rid>(exclusions),
            ShapeRid = PhysicsServer3D.AreaGetShape(detectionArea, 0),
            Transform = transform
        };

        return QueryAndSortIntersections(queryParams, space);
    }
    
    public static List<TypedCollision> GetOverlappingEntities(
        Rid detectionArea,
        Rid space,
        params Rid[] exclusions)
    {
        return GetOverlappingEntities(
            detectionArea,
            PhysicsServer3D.AreaGetTransform(detectionArea),
            space,
            exclusions
        );
    }

    // This submethod is no longer needed because there are no overloads that need it, but might as well keep it.
    private static List<TypedCollision> QueryAndSortIntersections(PhysicsShapeQueryParameters3D queryParams, Rid space)
    {
        var overlaps = PhysicsServer3D.SpaceGetDirectState(space).IntersectShape(queryParams);
        var results = new List<TypedCollision>();

        // GD.Print("Rids");
        foreach (var overlap in overlaps)
        {
            var objectRid = (Rid)overlap["rid"];
            // GD.Print(objectRid);
            if (CollisionRegistry.TryGetEntityInfo(objectRid, out var type, out EntityId entityId))
            {
                results.Add(new TypedCollision(type, entityId));
            }
        }

        return results;
    }
}
