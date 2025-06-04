using Godot;

namespace PrimerTools;

public static class PrimerMathUtils
{
    public static double DecayingExponentialWithMin(double initialDuration, double decayFactor, double minDuration, double step)
    {
        // TODO: Could fit this to an actual exponential instead of clamping, but who cares
        return Mathf.Max(initialDuration * Mathf.Pow(decayFactor, step), minDuration);
    }
    
    public static Vector3 SlerpThatWorks(Vector3 from, Vector3 to, float weight)
    {
        float s1 = from.Length();
        float s2 = to.Length();
        if (s1 == 0.0 || s2 == 0.0)
            return from.Lerp(to, weight);
        float num1 = Mathf.Lerp(s1, s2, weight);
        float angle = from.AngleTo(to);
        if (angle == 0.0)
            return from.Lerp(to, weight);
        Vector3 axis = from.Cross(to).Normalized();
        if (axis.Length() == 0)
        {
            // GD.Print($"Vectors that cross to zero: {from}, {to}");
            return from.Lerp(to, weight);
        }
        return from.Rotated(axis, angle * weight) * (num1 / s1);
    }
}