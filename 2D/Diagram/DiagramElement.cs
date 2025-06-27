using Godot;

namespace PrimerTools._2D.Diagram;

public abstract class DiagramElement
{
    public Vector2 Position { get; set; }

    protected DiagramElement(Vector2 position)
    {
        Position = position;
    }

    public abstract int GetShapeType();
    public abstract Rect2 GetBounds();
}

public class CircleElement : DiagramElement
{
    public float Radius { get; set; }

    public CircleElement(Vector2 center, float radius) : base(center)
    {
        Radius = radius;
    }

    public override int GetShapeType() => 0; // Circle type in shader

    public override Rect2 GetBounds()
    {
        // Return bounding box for the circle
        return new Rect2(Position - Vector2.One * Radius, Vector2.One * Radius * 2);
    }
}