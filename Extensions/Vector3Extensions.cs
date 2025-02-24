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

    public static Vector3 AllTheWayMinusLength(this Vector3 vec, Vector3 otherVec, float length)
    {
        // If length is greater than the distance between the two vectors. This returns a vector pointing away.
        var fullDisplacement = otherVec - vec;
        return vec + fullDisplacement.Normalized() * (fullDisplacement.Length() - length);
    }
}