using Godot;
using PrimerTools.TweenSystem;

namespace PrimerTools._2D.Diagram;

public static class DiagramElementExtensions
{
    public static CompositeStateChange Appear(this DiagramElement element)
    {
        var originalThickness = element.Style.Thickness;
        element.Style.Thickness = 0;
        var appearanceStateChange = new CompositeStateChange();
        appearanceStateChange.AddStateChange(
            new PropertyStateChange(element.Style, "Thickness", originalThickness)
        );
        
        var originalSmoothness = element.Style.Smoothness;
        element.Style.Smoothness = 0;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", originalSmoothness)
        );

        switch (element.ShapeData)
        {
            case CircleData circleData:
                var originalRadius = circleData.Radius;
                circleData.Radius = 0;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(circleData, "Radius", originalRadius),
                    delay: 0.25f
                );
                break;
            case ArrowData arrowData:
                
                var originalEnd = arrowData.End;
                arrowData.End = arrowData.Start;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(arrowData, "End", originalEnd).WithDuration(0.5f),
                    delay: 0.25
                );
                
                var originalHeadLength = arrowData.HeadLength;
                arrowData.HeadLength = 0;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(arrowData, "HeadLength", originalHeadLength).WithDuration(0.5f),
                    delay: 0.25f
                );
                
                break;
            default:
                GD.Print($"Appearance not implemented for shape type {element.ShapeData.GetType()}");
                break;
        }

        return appearanceStateChange;
    }
}