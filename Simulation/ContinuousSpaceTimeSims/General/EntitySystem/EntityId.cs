using System;
using Godot;

namespace PrimerTools.Simulation;

public readonly struct EntityId : IEquatable<EntityId>
{
    // This is all for type safety
    public readonly uint Value = 0;

    public EntityId(uint value)
    {
        Value = value;
    }

    public bool IsValid => Value > 0;

    public bool Equals(EntityId other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return obj is EntityId other && Equals(other);
    }

    public override int GetHashCode()
    {
        // To my knowledge, this is deterministic and should be fine.
        // But probably Value will never get big enough to test it anyway.
        return (int)Value;
    }

    public static bool operator ==(EntityId left, EntityId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EntityId left, EntityId right)
    {
        return !left.Equals(right);
    }
}
