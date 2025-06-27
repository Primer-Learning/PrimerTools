using Godot;

namespace PrimerTools._2D.Diagram;

public abstract class DiagramElement
{
    public Vector2 Position { get; set; }
    public float Padding { get; set; }

    protected DiagramElement(Vector2 position, float padding = 1)
    {
        Position = position;
        Padding = padding;
    }

    public abstract int GetShapeType();
    public abstract Rect2 GetBounds();
}

public class CircleElement : DiagramElement
{
    public float Radius { get; set; }
    public Vector2 Center { get; set; }

    public CircleElement(Vector2 center, float radius, float padding = 1) : base(center, padding)
    {
        Radius = radius;
        Center = center;
    }

    public override int GetShapeType() => 0; // Circle type in shader

    public override Rect2 GetBounds()
    {
        // Return bounding box for the circle
        return new Rect2(Position - Vector2.One * (Radius + Padding), Vector2.One * (Radius + Padding) * 2);
    }
}