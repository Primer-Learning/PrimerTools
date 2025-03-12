using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

/// <summary>
/// Manages a dictionary storing the IDataEntity type and entityId corresponding to an area Rid in the physics system
/// This allows entities to use area overlap queries to figure out which entities are nearby 
/// </summary>
public static class CollisionRegistry
{
    private static readonly Dictionary<Rid, (Type type, uint entityId)> GlobalBodyLookup = new();
    
    public static void RegisterBody(Rid rid, Type entityType, EntityId entityId)
    {
        GlobalBodyLookup[rid] = (entityType, entityId.Value);
    }

    // Keep the old method for compatibility with existing simulations
    public static void RegisterBody(Rid rid, Type entityType, uint entityId)
    {
        GlobalBodyLookup[rid] = (entityType, entityId);
    }
    
    public static void UnregisterBody(Rid rid)
    {
        GlobalBodyLookup.Remove(rid);
    }
    
    // New method that returns EntityId
    public static bool TryGetEntityInfo(Rid rid, out Type type, out EntityId entityId)
    {
        if (GlobalBodyLookup.TryGetValue(rid, out var info))
        {
            type = info.type;
            entityId = new EntityId(info.entityId);
            return true;
        }
        type = null;
        entityId = default;
        return false;
    }
}
