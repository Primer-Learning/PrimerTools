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
        appearanceStateChange.AddStateChangeWithDelay(
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
            case RectangleData rectData:
                var originalSize = rectData.Size;
                rectData.Size = Vector2.Zero;
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(rectData, "Size", originalSize).WithDuration(duration),
                    delay: duration / 2
                );
                break;
            default:
                GD.Print($"Appearance not implemented for shape type {element.ShapeData.GetType()}");
                break;
        }

        return appearanceStateChange;
    }
    
    public static CompositeStateChange Disappear(this DiagramElement element, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        var appearanceStateChange = new CompositeStateChange();
        switch (element.ShapeData)
        {
            case CircleData circleData:
                appearanceStateChange.AddStateChangeWithDelay(
                    new PropertyStateChange(circleData, "Radius", 0).WithDuration(duration)
                );
                break;
            case ArrowData arrowData:
                appearanceStateChange.AddStateChangeWithDelay(
                    new PropertyStateChange(arrowData, "Start", arrowData.End).WithDuration(duration)
                );
                appearanceStateChange.AddStateChangeInParallel(
                    new PropertyStateChange(arrowData, "HeadLength", 0).WithDuration(duration),
                    delay: duration / 2
                );
                
                break;
            default:
                GD.Print($"Appearance not implemented for shape type {element.ShapeData.GetType()}");
                break;
        }
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Thickness", 0).WithDuration(duration),
            delay: duration / 2
        );
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", 0).WithDuration(duration)
        );

        return appearanceStateChange;
    }
    
    public static CompositeStateChange Appear(this ShaderBracket element, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        var originalThickness = element.Style.Thickness;
        element.Style.Thickness = 0;
        var appearanceStateChange = new CompositeStateChange();
        appearanceStateChange.AddStateChangeWithDelay(
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
    
    public static CompositeStateChange Disappear(this ShaderBracket element, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        var appearanceStateChange = new CompositeStateChange();
        appearanceStateChange.AddStateChangeWithDelay(
            new PropertyStateChange(element.Style, "Thickness", 0).WithDuration(duration)
        );
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", 0).WithDuration(duration)
        );
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "LeftTipPosition", element.StemPosition).WithDuration(duration)
        );
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "RightTipPosition", element.StemPosition).WithDuration(duration)
        );
        
        return appearanceStateChange;
    }
    
    public static CompositeStateChange Appear(this ShaderArrow element, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        var originalThickness = element.Style.Thickness;
        element.Style.Thickness = 0;
        var appearanceStateChange = new CompositeStateChange();
        appearanceStateChange.AddStateChangeWithDelay(
            new PropertyStateChange(element.Style, "Thickness", originalThickness).WithDuration(duration)
        );
        
        var originalSmoothness = element.Style.Smoothness;
        element.Style.Smoothness = 0;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", originalSmoothness).WithDuration(duration)
        );
        
        // Just to pin this down. Without this, the start position when evaluated will be wherever the last 
        // one was set.
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "StartPosition", element.StartPosition).WithDuration(0),
            delay: 0
        );
        
        var originalEnd = element.EndPosition;
        element.EndPosition = element.StartPosition;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "EndPosition", originalEnd).WithDuration(duration),
            delay: duration / 2
        );
        
        var originalHeadLength = element.HeadLength;
        element.HeadLength = 0;
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "HeadLength", originalHeadLength).WithDuration(duration),
            delay: duration / 2
        );
        
        return appearanceStateChange;
    }
    
    // TODO: Make this not need currentHeadPosition
    // The issue is that state changes don't apply their changes when built. This is largely fine, perhaps even good
    // for reasons I don't recall. But the disappear method needs to know where the head currently is, and it doesn't
    // currently have a good way of knowing that.
    // One possibility is to make ShaderArrow and ShaderBracket, and everything else, work more like Graph,
    // where the class properties are separate from the visual node/shader properties they control. In that case,
    // the properties are updated inside of the sequence's Define method and are therefore accessible for building
    // transitions.
    // Another possibility could be to allow PropertyStateChange to optionally take a delegate instead of a value
    // which would allow it to figure out the value the first time it is evaluated, but complicating an otherwise fairly
    // simple class seems bad. 
    
    public static CompositeStateChange Disappear(this ShaderArrow element, double duration = Node3DStateChangeExtensions.DefaultDuration, Vector3? currentHeadPosition = null)
    {
        var appearanceStateChange = new CompositeStateChange();
        if (!currentHeadPosition.HasValue)
        {
            currentHeadPosition = element.EndPosition;
        }
        
        appearanceStateChange.AddStateChangeWithDelay(
            new PropertyStateChange(element, "StartPosition", currentHeadPosition.Value).WithDuration(duration)
        );
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element, "HeadLength", 0).WithDuration(duration),
            delay: duration / 2
        );
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Thickness", 0).WithDuration(duration),
            delay: duration / 2
        );
        
        appearanceStateChange.AddStateChangeInParallel(
            new PropertyStateChange(element.Style, "Smoothness", 0).WithDuration(duration)
        );
        
        return appearanceStateChange;
    }
}
