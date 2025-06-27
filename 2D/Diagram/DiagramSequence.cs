using Godot;
using PrimerTools.TweenSystem;

namespace PrimerTools._2D.Diagram;

public partial class DiagramSequence : StateChangeSequence
{
    protected override void Define()
    {
        // Create a diagram system
        var diagram = new DiagramSystem();
        diagram.ShaderPath = "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader";
        AddChild(diagram);
        
        // Create circle elements
        var circle1 = new CircleElement(new Vector2(-2, 0), 0.5f);
        var circle2 = new CircleElement(new Vector2(2, 0), 0.8f);
        
        // Create rectangle element
        var rect1 = new RectangleElement(new Vector2(0, -2), new Vector2(1.0f, 0.5f));
        
        diagram.AddElement(circle1);
        diagram.AddElement(circle2);
        diagram.AddElement(rect1);
        diagram.BuildDiagram();
        
        // Animate the circles
        // Move circle1 to the right
        AddStateChange(
            new PropertyStateChange(circle1, "Center", new Vector2(0, 0))
                .WithDuration(2)
        );
        
        // Grow circle2's radius
        AddStateChangeInParallel(
            new PropertyStateChange(circle2, "Radius", 1.5f)
                .WithDuration(2)
        );
        
        // Move both circles up
        AddStateChange(
            new PropertyStateChange(circle1, "Center", new Vector2(0, 2))
                .WithDuration(1.5)
        );
        AddStateChangeInParallel(
            new PropertyStateChange(circle2, "Center", new Vector2(2, 2))
                .WithDuration(1.5)
        );
        
        // Shrink circle1
        AddStateChange(
            new PropertyStateChange(circle1, "Radius", 0.2f)
                .WithDuration(1)
        );
        
        // Animate rectangle size
        AddStateChange(
            new PropertyStateChange(rect1, "Size", new Vector2(2.0f, 0.2f))
                .WithDuration(1.5)
        );
    }
}
