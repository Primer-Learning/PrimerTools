using Godot;

namespace PrimerTools;

public class Transform3DUtils
{
    public static Quaternion QuaternionFromEulerDeg(Vector3 euler)
    {
        return Quaternion.FromEuler(euler * Mathf.Pi / 180);
    }

    public static Basis BasisFromForwardAndUp(Vector3 forward, Vector3 up)
    {
        var left = up.Cross(forward).Normalized();
        up = forward.Cross(left).Normalized(); // Recompute up to ensure orthogonality
        return new Basis(left, up, forward);
    }
}