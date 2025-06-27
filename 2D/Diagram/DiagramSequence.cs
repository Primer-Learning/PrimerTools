using Godot;
using PrimerTools.TweenSystem;

namespace PrimerTools._2D.Diagram;

public partial class DiagramSequence : StateChangeSequence
{
    protected override void Define()
    {
        // Create a diagram system
        var diagram = new DiagramSystem();
        AddChild(diagram);
        
        // Create shape data
        var circle1Data = new CircleData(new Vector2(-2, 0), 0.5f);
        var circle2Data = new CircleData(new Vector2(2, 0), 0.8f);
        var rectData = new RectangleData(new Vector2(0, -2), new Vector2(1.0f, 0.5f));
        var lineData = new LineData(new Vector2(-3, 1), new Vector2(-1, 3));
        var triangleData = new TriangleData(
            new Vector2(3, 1),
            new Vector2(2, 3),
            new Vector2(4, 3)
        );
        
        // Create diagram elements
        var circle1 = new DiagramElement(circle1Data);
        var circle2 = new DiagramElement(circle2Data);
        var rect1 = new DiagramElement(rectData);
        var line1 = new DiagramElement(lineData);
        var triangle1 = new DiagramElement(triangleData);
        
        diagram.AddElement(circle1);
        diagram.AddElement(circle2);
        diagram.AddElement(rect1);
        diagram.AddElement(line1);
        diagram.AddElement(triangle1);
        diagram.BuildDiagram();
        
        // Animate the shapes directly
        // Move circle1 to the right
        AddStateChange(
            new PropertyStateChange(circle1Data, "Center", new Vector2(0, 0))
                .WithDuration(2)
        );
        
        // Grow circle2's radius
        AddStateChangeInParallel(
            new PropertyStateChange(circle2Data, "Radius", 1.5f)
                .WithDuration(2)
        );
        
        // Move both circles up
        AddStateChange(
            new PropertyStateChange(circle1Data, "Center", new Vector2(0, 2))
                .WithDuration(1.5)
        );
        AddStateChangeInParallel(
            new PropertyStateChange(circle2Data, "Center", new Vector2(2, 2))
                .WithDuration(1.5)
        );
        
        // Shrink circle1
        AddStateChange(
            new PropertyStateChange(circle1Data, "Radius", 0.2f)
                .WithDuration(1)
        );
        
        // Animate rectangle size
        AddStateChange(
            new PropertyStateChange(rectData, "Size", new Vector2(2.0f, 0.2f))
                .WithDuration(1.5)
        );
        
        // Animate line endpoints
        AddStateChange(
            new PropertyStateChange(lineData, "PointB", new Vector2(1, 1))
                .WithDuration(2)
        );
        
        // Animate triangle vertices
        AddStateChangeInParallel(
            new PropertyStateChange(triangleData, "PointA", new Vector2(3, -1))
                .WithDuration(2)
        );
    }
}
