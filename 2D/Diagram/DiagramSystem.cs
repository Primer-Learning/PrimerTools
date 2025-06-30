using Godot;
using PrimerTools;
using PrimerTools._2D.Diagram;

[Tool]
public partial class DiagramSystem : Node3D
{
    public ShapeStyle DefaultStyle { get; set; } = new ShapeStyle();
    
    public DiagramSystem() {}

    [ExportToolButton("Create")]
    private Callable Create => Callable.From(RebuildDiagram);

    protected virtual void DefineDiagram() {}

    public void AddElement(DiagramElement element)
    {
        if (element.Style == null)
        {
            element.Style = DefaultStyle.Clone();
        }
        
        element.CreateMesh(this);
    }

    public void RebuildDiagram()
    {
        // Clear existing meshes
        foreach (var child in GetChildren())
        {
            if (Engine.IsEditorHint())
            {
                child.Free();
            }
            else
            {
                child.QueueFree();
            }
        }

        DefineDiagram();
        BuildDiagram();
        
        if (Engine.IsEditorHint()) this.MakeSelfAndChildrenLocal();
    }

    public void BuildDiagram() {}
}
