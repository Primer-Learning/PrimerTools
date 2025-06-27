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

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            BuildDiagram();
        }
    }

    [ExportToolButton("Create")]
    private Callable Create => Callable.From(BuildDiagram);

    protected virtual void DefineDiagram()
    {
        // Override this method to define your diagram
        // Example:
        // AddElement(new CircleElement(new Vector2(0, 0), 0.5f));
        // AddElement(new CircleElement(new Vector2(2, 0), 0.5f));
    }

    public void AddElement(DiagramElement element)
    {
        _elements.Add(element);
    }

    public void BuildDiagram()
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

        // Define the diagram
        DefineDiagram();

        // Create mesh instances for each element
        foreach (var element in _elements)
        {
            CreateMeshForElement(element);
        }
        
        this.MakeSelfAndChildrenLocal();
    }
    
    private void CreateMeshForElement(DiagramElement element)
    {
        // Get element bounds
        var bounds = element.GetBounds();

        // Create mesh instance
        var meshInstance = new MeshInstance3D();
        meshInstance.RotationDegrees = new Vector3(90, 0, 0);
        AddChild(meshInstance);
        meshInstance.Name = $"DiagramElement_{element.GetType().Name}";

        // Create plane mesh sized to fit the element's bounding box
        var planeMesh = new PlaneMesh();
        planeMesh.Size = bounds.Size;
        planeMesh.SubdivideWidth = 1;
        planeMesh.SubdivideDepth = 1;
        meshInstance.Mesh = planeMesh;

        // Position the mesh so its center aligns with the bounds center
        meshInstance.Position = new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y,
            0);

        // Create and configure shader material
        var shader = GD.Load<Shader>(ShaderPath);
        if (shader == null)
        {
            GD.PrintErr($"Failed to load shader at path: {ShaderPath}");
            return;
        }

        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = shader;

        // Set common shader parameters
        shaderMaterial.SetShaderParameter("shape_color", DefaultShapeColor);
        shaderMaterial.SetShaderParameter("background_color", DefaultBackgroundColor);
        shaderMaterial.SetShaderParameter("thickness", DefaultThickness);
        shaderMaterial.SetShaderParameter("smoothness", DefaultSmoothness);
        shaderMaterial.SetShaderParameter("shape_type", element.GetShapeType());

        // Set shape center in object space (relative to mesh center)
        var shapeCenter = element.Position - bounds.GetCenter();
        shaderMaterial.SetShaderParameter("shape_center", shapeCenter);

        // Set shape-specific parameters
        if (element is CircleElement circle)
        {
            shaderMaterial.SetShaderParameter("radius", circle.Radius);
            shaderMaterial.SetShaderParameter("shape_center", circle.Center);
        }
        // Add other shape types as needed

        meshInstance.SetSurfaceOverrideMaterial(0, shaderMaterial);
    }

    // private void CreateMeshForElement(DiagramElement element)
    // {
    //     // Get element bounds
    //     var bounds = element.GetBounds();
    //
    //     // Create mesh instance
    //     var meshInstance = new MeshInstance3D();
    //     meshInstance.Name = $"DiagramElement_{element.GetType().Name}";
    //     meshInstance.RotationDegrees = new Vector3(90, 0, 0);
    //     AddChild(meshInstance);
    //
    //     // Create plane mesh sized to fit the element
    //     var planeMesh = new PlaneMesh();
    //     planeMesh.Size = bounds.Size;
    //     planeMesh.SubdivideWidth = 1;
    //     planeMesh.SubdivideDepth = 1;
    //     meshInstance.Mesh = planeMesh;
    //
    //     // Position the mesh in local space
    //     meshInstance.Position = new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y, 0);
    //
    //     // Create and configure shader material
    //     var shader = GD.Load<Shader>(ShaderPath);
    //     if (shader == null)
    //     {
    //         GD.PrintErr($"Failed to load shader at path: {ShaderPath}");
    //         return;
    //     }
    //
    //     var shaderMaterial = new ShaderMaterial();
    //     shaderMaterial.Shader = shader;
    //
    //     // Set shader parameters
    //     shaderMaterial.SetShaderParameter("shape_color", DefaultShapeColor);
    //     shaderMaterial.SetShaderParameter("background_color", DefaultBackgroundColor);
    //     shaderMaterial.SetShaderParameter("thickness", DefaultThickness);
    //     shaderMaterial.SetShaderParameter("smoothness", DefaultSmoothness);
    //     shaderMaterial.SetShaderParameter("shape_type", element.GetShapeType());
    //
    //     // Calculate UV offset to position the shape correctly
    //     // The shader expects UV coordinates from 0-1, which map to -1 to 1 in shader space
    //     // We need to transform the element's position relative to the mesh's bounds
    //     var relativePos = (element.Position - bounds.GetCenter()) / bounds.Size;
    //
    //     meshInstance.MaterialOverride = shaderMaterial;
    // }
}