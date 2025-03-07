using System;
using Godot;

// Claude wrote these
public static class Easing
{
    /// <summary>
    /// Applies cubic easing to a value between 0 and 1.
    /// Values outside this range will be clamped.
    /// </summary>
    /// <param name="t">Input value (ideally between 0 and 1)</param>
    /// <returns>Eased value between 0 and 1</returns>
    public static float CubicEaseIn(float t)
    {
        // Clamp the input value between 0 and 1
        t = Math.Clamp(t, 0f, 1f);
        
        // Apply cubic easing: t³
        return t * t * t;
    }
    
    /// <summary>
    /// Applies cubic ease-in-out to a value between 0 and 1.
    /// Values outside this range will be clamped.
    /// </summary>
    /// <param name="t">Input value (ideally between 0 and 1)</param>
    /// <returns>Eased value between 0 and 1</returns>
    public static float CubicEaseInOut(float t)
    {
        // Clamp the input value between 0 and 1
        t = Math.Clamp(t, 0f, 1f);
        
        // Apply cubic ease-in-out
        return t < 0.5
            ? 4 * t * t * t 
            : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
    
    /// <summary>
    /// Applies cubic ease-out to a value between 0 and 1.
    /// Values outside this range will be clamped.
    /// </summary>
    /// <param name="t">Input value (ideally between 0 and 1)</param>
    /// <returns>Eased value between 0 and 1</returns>
    public static float CubicEaseOut(float t)
    {
        // Clamp the input value between 0 and 1
        t = Math.Clamp(t, 0f, 1f);
        
        // Apply cubic ease-out: 1-(1-t)³
        return 1 - Mathf.Pow(1 - t, 3);
    }
    
    /// <summary>
    /// Creates an easing function with configurable pauses at the beginning and end.
    /// Returns 0 when input is below startThreshold, returns 1 when input is above endThreshold,
    /// and performs easing between these thresholds.
    /// </summary>
    /// <param name="t">Input value (ideally between 0 and 1)</param>
    /// <param name="startThreshold">Value below which the output is 0 (e.g., 0.25)</param>
    /// <param name="endThreshold">Value above which the output is 1 (e.g., 0.75)</param>
    /// <param name="easingFunc">The easing function to use between thresholds (defaults to linear)</param>
    /// <returns>Eased value between 0 and 1 with pauses</returns>
    public static float EaseWithPause(float t, float startThreshold, float endThreshold, Func<float, float> easingFunc = null)
    {
        // Clamp the input value between 0 and 1
        t = Math.Clamp(t, 0f, 1f);
        
        // Default to linear easing if no function provided
        easingFunc ??= (x) => x;
        
        // Handle cases outside the easing range
        if (t <= startThreshold)
            return 0f;
        
        if (t >= endThreshold)
            return 1f;
        
        // Normalize t to 0-1 range between thresholds
        float normalizedT = (t - startThreshold) / (endThreshold - startThreshold);
        
        // Apply the provided easing function
        return easingFunc(normalizedT);
    }
}