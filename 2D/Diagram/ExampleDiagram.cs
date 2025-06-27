using Godot;

namespace PrimerTools._2D.Diagram;

[Tool]
public partial class ExampleDiagram : DiagramSystem
{
    protected override void DefineDiagram()
    {
        // Add two circles
        AddElement(new CircleElement(new Vector2(-1, 1), 0.7f));
        AddElement(new CircleElement(new Vector2(1, 0), 3f));
    }
}