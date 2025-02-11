using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using GladiatorManager.addons.PrimerTools;
using Godot;

namespace PrimerTools;

public static class PrimerColor
{
    public static Color Blue => PrimerConfig.Instance.Colors.Blue;
    public static Color Orange => PrimerConfig.Instance.Colors.Orange;
    public static Color Yellow => PrimerConfig.Instance.Colors.Yellow;
    public static Color Red => PrimerConfig.Instance.Colors.Red;
    public static Color Green => PrimerConfig.Instance.Colors.Green;
    public static Color Purple => PrimerConfig.Instance.Colors.Purple;
    public static Color Gray => PrimerConfig.Instance.Colors.Gray;
    public static Color White => PrimerConfig.Instance.Colors.White;
    public static Color Black => PrimerConfig.Instance.Colors.Black;

    public static readonly Color[] Rainbow = {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
    };
    
    // Color mixing
    public static Color JuicyInterpolate(Color a, Color b, float t, float juiciness = 0.5f)
    {
        GD.PushWarning("JuicyInterpolate is deprecated. Use InterpolateInLinearSpace instead.");
        return InterpolateInLinearSpace(a, b, t);
    }
    public static Color InterpolateInLinearSpace(Color a, Color b, float t)
    {
        return MixColorsByWeight(new[] { a, b }, new[] { 1 - t, t });
    }

    public static Color OneMinusTheColor(Color theColor)
    {
        return new Color(1 - theColor.R, 1 - theColor.G, 1 - theColor.B, theColor.A);
    }

    public static Color MixColorsByWeight(Color[] colors, float[] weights, bool subtractive = true, bool messWithBrightness = true)
    {
        float r = 0;
        float g = 0;
        float b = 0;
        float a = 0;
        float v = 0;
        if (colors.Length != weights.Length) GD.PrintErr($"MixColorsByWeight received arrays of different lengths. Colors: {colors.Length}, Weights: {weights.Length}");

        var normalizationFactor = weights.Sum();
        
        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            
            if (subtractive)
            {
                color = OneMinusTheColor(color);
            }

            var linearColor = color.SrgbToLinear();
            r += linearColor.R * weights[i] / normalizationFactor;
            g += linearColor.G * weights[i] / normalizationFactor;
            b += linearColor.B * weights[i] / normalizationFactor;
            a += linearColor.A * weights[i] / normalizationFactor;
            v += linearColor.V * weights[i] / normalizationFactor;
        }

        var mixedColor = new Color(r, g, b, a);
        if (messWithBrightness) { mixedColor.V = v; }
        var finalishColor = mixedColor.LinearToSrgb();
        if (subtractive) finalishColor = OneMinusTheColor(finalishColor);
        return finalishColor;
    }
}