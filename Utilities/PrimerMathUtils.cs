using Godot;

namespace PrimerTools;

public static class PrimerMathUtils
{
    public static float DecayingExponentialWithMin(float initialDuration, float decayFactor, float minDuration, float step)
    {
        // TODO: Could fit this to an actual exponential instead of clamping, but who cares
        return Mathf.Max(initialDuration * Mathf.Pow(decayFactor, step), minDuration);
    }
}