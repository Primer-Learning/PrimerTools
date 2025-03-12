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
    
    public static List<TypedCollision> GetOverlappingEntitiesWithArea(
        Rid detectionArea,
        Transform3D transform,
        Rid space,
        params Rid[] exclusions)
    {
        var queryParams = new PhysicsShapeQueryParameters3D
        {
            CollideWithAreas = true,
            CollideWithBodies = true,
            Exclude = new Array<Rid>(exclusions),
            ShapeRid = PhysicsServer3D.AreaGetShape(detectionArea, 0),
            Transform = transform
        };

        return QueryAndSortIntersections(queryParams, space);
    }
    public static List<TypedCollision> GetEntitiesWithinRange(
        float radius,
        Transform3D transform,
        Rid space,
        params Rid[] exclusions)
    {
        var queryParams = new PhysicsShapeQueryParameters3D
        {
            CollideWithAreas = true,
            CollideWithBodies = true,
            Exclude = new Array<Rid>(exclusions),
            ShapeRid = new SphereShape3D() {Radius = radius}.GetRid(),
            Transform = transform
        };

        return QueryAndSortIntersections(queryParams, space);
    }
    
    public static List<TypedCollision> GetOverlappingEntitiesWithArea(
        Rid detectionArea,
        Rid space,
        params Rid[] exclusions)
    {
        return GetOverlappingEntitiesWithArea(
            detectionArea,
            PhysicsServer3D.AreaGetTransform(detectionArea),
            space,
            exclusions
        );
    }
    
    // There's not a clean way to figure out what an Rid corresponds to, that I know of
    // So we have separate methods for body and area collision detection
    // One idea for making this less bad is to have the physics components call these themselves
    public static List<TypedCollision> GetOverlappingEntitiesWithBody(
        Rid detectionBody,
        Transform3D transform,
        Rid space,
        params Rid[] exclusions)
    {
        var queryParams = new PhysicsShapeQueryParameters3D
        {
            CollideWithAreas = true,
            CollideWithBodies = true,
            Exclude = new Array<Rid>(exclusions),
            ShapeRid = PhysicsServer3D.BodyGetShape(detectionBody, 0),
            Transform = transform
        };

        return QueryAndSortIntersections(queryParams, space);
    }
    public static List<TypedCollision> GetOverlappingEntitiesWithBody(
        Rid detectionBody,
        Rid space,
        params Rid[] exclusions)
    {
        return GetOverlappingEntitiesWithBody(
            detectionBody,
            (Transform3D)PhysicsServer3D.BodyGetState(detectionBody, PhysicsServer3D.BodyState.Transform),
            space,
            exclusions
        );
    }

    // This submethod is no longer needed because there are no overloads that need it, but might as well keep it.
    private static List<TypedCollision> QueryAndSortIntersections(PhysicsShapeQueryParameters3D queryParams, Rid space)
    {
        const int maxIntersections = 128;
        var overlaps = PhysicsServer3D.SpaceGetDirectState(space).IntersectShape(queryParams, maxResults: maxIntersections);
        if (overlaps.Count == maxIntersections) GD.PushWarning($"Hit the max number of intersections ({maxIntersections}). FYI.");
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
