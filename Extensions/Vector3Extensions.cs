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

    /// <summary>
    /// Finds a point between vec and otherVec, cutLength away from otherVec. Effectively travels from vec to otherVec,
    /// but stops short by cutLength. Similar to Lerp, but with an absolute length rather than a fraction.
    /// This isn't clamped, so if cutLength is negative or larger than the distance between the two vectors,
    /// the returned vector will be outside the region between the vec and otherVec.
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="otherVec"></param>
    /// <param name="cutLength"></param>
    /// <returns></returns>
    public static Vector3 AllTheWayMinusLength(this Vector3 vec, Vector3 otherVec, float cutLength)
    {
        // If length is greater than the distance between the two vectors. This returns a vector pointing away.
        return vec + (otherVec - vec).ShortenLengthBy(cutLength);
        
        // var fullDisplacement = otherVec - vec;
        // var fullLength = fullDisplacement.Length();
        // return vec + fullDisplacement / fullLength * (fullLength - cutLength);
    }

    public static Vector3 ShortenLengthBy(this Vector3 vec, float cutLength)
    {
        var fullLength = vec.Length();
        return vec / fullLength * (fullLength - cutLength);
    }
}