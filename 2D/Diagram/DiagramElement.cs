using Godot;

namespace PrimerTools._2D.Diagram;

public partial class DiagramElement : Node
{
    private ShapeData _shapeData;
    private MeshInstance3D _meshInstance;
    private ShaderMaterial _shaderMaterial;
    private DiagramSystem _parentSystem;
    
    private float _padding = 1f;
    public float Padding 
    { 
        get => _padding;
        set
        {
            _padding = value;
            UpdateMeshTransform();
        }
    }

    public ShapeData ShapeData => _shapeData;

    public DiagramElement(ShapeData shapeData, float padding = 1)
    {
        _shapeData = shapeData;
        _padding = padding;
        
        // Subscribe to shape changes
        _shapeData.ShapeChanged += OnShapeChanged;
    }

    public override void _ExitTree()
    {
        // Unsubscribe when removed from tree
        if (_shapeData != null)
        {
            _shapeData.ShapeChanged -= OnShapeChanged;
        }
    }

    private void OnShapeChanged()
    {
        UpdateMeshTransform();
        UpdateShaderParameters();
    }

    private string GetShaderPath()
    {
        return _shapeData.GetShapeType() switch
        {
            0 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader",
            1 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/rectangle_shader.gdshader",
            2 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/line_shader.gdshader",
            3 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/triangle_shader.gdshader",
            _ => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader"
        };
    }
    
    public void CreateMesh(DiagramSystem parentSystem)
    {
        _parentSystem = parentSystem;
        
        // Create mesh instance
        _meshInstance = new MeshInstance3D();
        _meshInstance.RotationDegrees = new Vector3(90, 0, 0);
        parentSystem.AddChild(_meshInstance);
        _meshInstance.Name = $"DiagramElement_{_shapeData.GetType().Name}";
        
        // Create plane mesh
        var bounds = _shapeData.GetBounds(_padding);
        var planeMesh = new PlaneMesh();
        planeMesh.Size = bounds.Size;
        planeMesh.SubdivideWidth = 1;
        planeMesh.SubdivideDepth = 1;
        _meshInstance.Mesh = planeMesh;
        
        // Create shader material
        var shaderPath = GetShaderPath();
        var shader = GD.Load<Shader>(shaderPath);
        if (shader == null)
        {
            GD.PrintErr($"Failed to load shader at path: {shaderPath}");
            return;
        }
        
        _shaderMaterial = new ShaderMaterial();
        _shaderMaterial.Shader = shader;
        
        // Set common shader parameters
        _shaderMaterial.SetShaderParameter("shape_color", parentSystem.DefaultShapeColor);
        _shaderMaterial.SetShaderParameter("background_color", parentSystem.DefaultBackgroundColor);
        _shaderMaterial.SetShaderParameter("thickness", parentSystem.DefaultThickness);
        _shaderMaterial.SetShaderParameter("smoothness", parentSystem.DefaultSmoothness);
        
        _meshInstance.SetSurfaceOverrideMaterial(0, _shaderMaterial);
        
        // Update transform and shader parameters
        UpdateMeshTransform();
        UpdateShaderParameters();
    }
    
    private void UpdateMeshTransform()
    {
        if (_meshInstance == null) return;
        
        var bounds = _shapeData.GetBounds(_padding);
        _meshInstance.Position = new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y, 0);
        
        // Update mesh size if needed
        if (_meshInstance.Mesh is PlaneMesh planeMesh)
        {
            planeMesh.Size = bounds.Size;
        }
    }
    
    private void UpdateShaderParameters()
    {
        if (_shaderMaterial == null) return;
        
        _shapeData.SetShaderParameters(_shaderMaterial);
        
        // Set inner thickness for shapes that support it
        if (_shapeData is CircleData || _shapeData is RectangleData || _shapeData is TriangleData)
        {
            _shaderMaterial.SetShaderParameter("inner_thickness", _parentSystem?.DefaultThickness ?? 0.01f);
        }
    }
}
