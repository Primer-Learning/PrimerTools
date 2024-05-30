using Godot;

namespace PrimerTools;

public static class PrimerMathUtils
{
    public static double DecayingExponentialWithMin(double initialDuration, double decayFactor, double minDuration, double step)
    {
        // TODO: Could fit this to an actual exponential instead of clamping, but who cares
        return Mathf.Max(initialDuration * Mathf.Pow(decayFactor, step), minDuration);
    }
}