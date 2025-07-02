using Godot;

namespace PrimerTools._2D.Diagram;

public partial class DiagramElement : Node
{
    [Export] private ShapeData _shapeData;
    private MeshInstance3D _meshInstance;
    private ShaderMaterial _shaderMaterial;
    private DiagramSystem _parentSystem;
    private ShapeStyle _style;
    
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
    
    public ShapeStyle Style 
    { 
        get => _style;
        set
        {
            if (_style != null)
                _style.StyleChanged -= OnStyleChanged;
            
            _style = value;
            
            if (_style != null)
                _style.StyleChanged += OnStyleChanged;
            
            UpdateShaderParameters();
        }
    }

    public DiagramElement(ShapeData shapeData, float padding = 1, ShapeStyle style = null)
    {
        _padding = padding;
        
        _shapeData = shapeData;
        _shapeData.ShapeChanged += OnShapeChanged;

        if (style != null) Style = style;
    }

    public override void _ExitTree()
    {
        // Unsubscribe when removed from tree
        if (_shapeData != null)
        {
            _shapeData.ShapeChanged -= OnShapeChanged;
        }
        if (_style != null)
        {
            _style.StyleChanged -= OnStyleChanged;
        }
    }

    private void OnShapeChanged()
    {
        UpdateMeshTransform();
        UpdateShaderParameters();
    }
    
    private void OnStyleChanged()
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
            4 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/arrow_shader.gdshader",
            5 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/curly_bracket_shader.gdshader",
            99 => "res://addons/PrimerTools/2D/Diagram/ShapeShaders/composite_shader.gdshader",
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
        var bounds = GetPaddedBounds();
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
        _style.ApplyToShader(_shaderMaterial);
        
        _meshInstance.SetSurfaceOverrideMaterial(0, _shaderMaterial);
        
        // Update transform and shader parameters
        UpdateMeshTransform();
        UpdateShaderParameters();
    }
    
    private Rect2 GetPaddedBounds()
    {
        var bounds = _shapeData.GetBounds();
        
        // Calculate total padding including style thickness
        var totalPadding = _padding;
        if (_style != null)
        {
            totalPadding += _style.Thickness;
        }
        
        // Grow bounds by total padding
        return bounds.Grow(totalPadding);
    }
    
    private void UpdateMeshTransform()
    {
        if (_meshInstance == null) return;
        
        var bounds = GetPaddedBounds();
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
        
        // Apply style parameters
        if (_style != null)
        {
            _style.ApplyToShader(_shaderMaterial);
        }
    }
}
