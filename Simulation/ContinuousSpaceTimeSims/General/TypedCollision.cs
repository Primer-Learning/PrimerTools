using System;
using Godot;

namespace PrimerTools.Simulation;

public readonly struct TypedCollision
{
    public readonly Type EntityType;
    public readonly EntityId EntityId;

    public TypedCollision(Type entityType, EntityId entityId)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
