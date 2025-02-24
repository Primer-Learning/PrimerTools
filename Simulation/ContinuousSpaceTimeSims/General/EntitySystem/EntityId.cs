using System;

namespace PrimerTools.Simulation;

public readonly struct EntityId : IEquatable<EntityId>
{
    // This is all for type safety
    public readonly int Value = -1;

    public EntityId(int value)
    {
        Value = value;
    }

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
        return Value;
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
