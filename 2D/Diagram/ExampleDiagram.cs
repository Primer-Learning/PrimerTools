using Godot;

namespace PrimerTools._2D.Diagram;

[Tool]
public partial class ExampleDiagram : DiagramSystem
{
    protected override void DefineDiagram()
    {
        // Add two circles
        AddElement(new CircleElement(new Vector2(-1, 1), 1f));
        AddElement(new CircleElement(new Vector2(3, 0), 3f));
    }
}