using Godot;
using PrimerTools.TweenSystem;

namespace PrimerTools._2D.Diagram;

public static class DiagramElementExtensions
{
    public static CompositeStateChange Appear(this DiagramElement element, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        var originalThickness = element.Style.Thickness;
        element.Style.Thickness = 0;
        var appearanceStateChange = new CompositeStateChange();
        appearanceStateChange.AddStateChange(
            new PropertyStateChange(element.Style, "Thickness", originalThickness).WithDuration(duration)
        );
        
        var originalSmoothness = element.Style.Smoothness;
        element.Style.Smoothness = 0;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", originalSmoothness).WithDuration(duration)
        );

        switch (element.ShapeData)
        {
            case CircleData circleData:
                var originalRadius = circleData.Radius;
                circleData.Radius = 0;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(circleData, "Radius", originalRadius).WithDuration(duration),
                    delay: duration / 2
                );
                break;
            case ArrowData arrowData:
                
                var originalEnd = arrowData.End;
                arrowData.End = arrowData.Start;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(arrowData, "End", originalEnd).WithDuration(duration),
                    delay: duration / 2
                );
                
                var originalHeadLength = arrowData.HeadLength;
                arrowData.HeadLength = 0;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(arrowData, "HeadLength", originalHeadLength).WithDuration(duration),
                    delay: duration / 2
                );
                
                break;
            default:
                GD.Print($"Appearance not implemented for shape type {element.ShapeData.GetType()}");
                break;
        }

        return appearanceStateChange;
    }
    
    public static CompositeStateChange Appear(this ShaderBracket element, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        var originalThickness = element.Style.Thickness;
        element.Style.Thickness = 0;
        var appearanceStateChange = new CompositeStateChange();
        appearanceStateChange.AddStateChange(
            new PropertyStateChange(element.Style, "Thickness", originalThickness).WithDuration(duration)
        );
        
        var originalSmoothness = element.Style.Smoothness;
        element.Style.Smoothness = 0;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", originalSmoothness).WithDuration(duration)
        );
        
        var originalLeft = element.LeftTipPosition;
        element.LeftTipPosition = element.StemPosition;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "LeftTipPosition", originalLeft).WithDuration(duration)
        );
        var originalRight = element.RightTipPosition;
        element.RightTipPosition = element.StemPosition;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "RightTipPosition", originalRight).WithDuration(duration)
        );
           

        return appearanceStateChange;
    }
}