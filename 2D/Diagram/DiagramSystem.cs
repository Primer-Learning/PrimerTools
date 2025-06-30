using Godot;
using System.Collections.Generic;
using Godot.Collections;
using PrimerTools;
using PrimerTools._2D.Diagram;

[Tool]
public partial class DiagramSystem : Node3D
{
    private string ShaderPath = "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader";
    public Color DefaultBackgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    public float DefaultSmoothness = 0.01f;

    private ShapeStyle DefaultStyle { get; set; }

    private List<DiagramElement> _elements = new List<DiagramElement>();
    
    public DiagramSystem()
    {
        DefaultStyle = new ShapeStyle(0.1f, 0.01f, new Color(1.0f, 0.5f, 0.0f, 1.0f));
    }

    [ExportToolButton("Create")]
    private Callable Create => Callable.From(RebuildDiagram);

    protected virtual void DefineDiagram() {}

    public void AddElement(DiagramElement element)
    {
        _elements.Add(element);
        
        // If element doesn't have a style, use a clone of the default
        if (element.Style == null)
        {
            element.Style = DefaultStyle.Clone();
        }
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
        _elements.Clear();

        DefineDiagram();
        BuildDiagram();
        
        if (Engine.IsEditorHint()) this.MakeSelfAndChildrenLocal();
    }

    public void BuildDiagram()
    {
        foreach (var element in _elements)
        {
            CreateMeshForElement(element);
        }
    }
    
    private void CreateMeshForElement(DiagramElement element)
    {
        element.CreateMesh(this);
    }
}
