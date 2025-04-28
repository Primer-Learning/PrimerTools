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
    
    public static Vector3 RandomXYZEulerRotationVector(Rng rng = null)
    {
        var x = rng.RangeFloat(-1, 1);
        x = Mathf.Asin(x);
        var y = rng.RangeFloat(0, Mathf.Pi * 2); 
        var z = rng.RangeFloat(0, Mathf.Pi * 2); 
        
        return new Vector3(x, y, z);
    }

    public static Vector3 RandomlyRotatedUnitVector(Rng rng = null)
    {
        return Quaternion.FromEuler(RandomXYZEulerRotationVector(rng)) * Vector3.Forward;
    }

}