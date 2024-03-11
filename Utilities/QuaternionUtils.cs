using Godot;

namespace PrimerTools;

public class QuaternionUtils
{
    public static Quaternion FromEulerDeg(Vector3 euler)
    {
        return Quaternion.FromEuler(euler * Mathf.Pi / 180);
    }
}