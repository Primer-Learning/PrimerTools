using Godot;

namespace PrimerTools;

public static class Vector3Extensions
{
    public static bool IsLengthGreaterThan(this Vector3 vec, float length)
    {
        return vec.LengthSquared() > length * length;
    }
    public static bool IsLengthLessThan(this Vector3 vec, float length)
    {
        return vec.LengthSquared() < length * length;
    }
}