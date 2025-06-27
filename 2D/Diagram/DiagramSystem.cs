using Godot;
using System.Collections.Generic;
using Godot.Collections;
using PrimerTools;
using PrimerTools._2D.Diagram;

[Tool]
public partial class DiagramSystem : Node3D
{
    public string ShaderPath = "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader";
    [Export] public Color DefaultShapeColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    [Export] public Color DefaultBackgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    [Export] public float DefaultThickness = 0.1f;
    [Export] public float DefaultSmoothness = 0.01f;

    private List<DiagramElement> _elements = new List<DiagramElement>();

    [ExportToolButton("Create")]
    private Callable Create => Callable.From(RebuildDiagram);

    protected virtual void DefineDiagram() {}

    public void AddElement(DiagramElement element)
    {
        _elements.Add(element);
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
