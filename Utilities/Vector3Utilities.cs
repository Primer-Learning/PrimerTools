using Godot;

namespace PrimerTools.Utilities;

public static class VectorUtilities
{
    public static Vector2 RandomVector2(Vector2 min, Vector2 max, Rng rng = null)
    {
        return new Vector2(
            rng.RangeFloat(min.X, max.X),
            rng.RangeFloat(min.Y, max.Y)
        );
    }
    
    public static Vector3 RandomVector3(Vector3 min, Vector3 max, Rng rng = null)
    {
        return new Vector3(
            rng.RangeFloat(min.X, max.X),
            rng.RangeFloat(min.Y, max.Y),
            rng.RangeFloat(min.Z, max.Z)
        );
    }
}