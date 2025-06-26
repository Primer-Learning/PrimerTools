using Godot;

[Tool]
public partial class SDFPlaneCreator : Node3D
{
    [Export] public string ShaderPath = "res://addons/PrimerTools/2D/sdf_shape_2d.gdshader";
    [Export] public Vector2 PlaneSize = new Vector2(2.0f, 2.0f);
    
    // Shader parameters
    [Export] public Color ShapeColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    [Export] public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    [Export] public float Thickness = 0.1f;
    [Export] public float Smoothness = 0.01f;
    [Export] public int ShapeType = 0; // 0: circle, 1: box, 2: hexagon, 3: star, 4: triangle
    
    // Triangle points (only used when shapeType = 4)
    [Export] public Vector2 TrianglePoint1 = new Vector2(0.0f, -0.7f);
    [Export] public Vector2 TrianglePoint2 = new Vector2(-0.6f, 0.5f);
    [Export] public Vector2 TrianglePoint3 = new Vector2(0.6f, 0.5f);
    
    private MeshInstance3D _meshInstance;
    private ShaderMaterial _shaderMaterial;

    [ExportToolButton("Create")]
    private Callable Create => Callable.From(CreateSDFPlane);
    
    public override void _Ready()
    {
        CreateSDFPlane();
    }
    
    private void CreateSDFPlane()
    {
        _meshInstance?.Free();
        
        // Create MeshInstance3D node
        _meshInstance = new MeshInstance3D();
        _meshInstance.Name = "SDFPlane";
        AddChild(_meshInstance);
        
        // Create PlaneMesh
        var planeMesh = new PlaneMesh();
        planeMesh.Size = PlaneSize;
        planeMesh.SubdivideWidth = 1;
        planeMesh.SubdivideDepth = 1;
        _meshInstance.Mesh = planeMesh;
        
        // Load shader and create ShaderMaterial
        var shader = GD.Load<Shader>(ShaderPath);
        if (shader == null)
        {
            GD.PrintErr($"Failed to load shader at path: {ShaderPath}");
            return;
        }
        
        _shaderMaterial = new ShaderMaterial();
        _shaderMaterial.Shader = shader;
        
        // Apply the material to the mesh
        _meshInstance.MaterialOverride = _shaderMaterial;
        
        // Set shader parameters
        UpdateShaderParameters();
    }
    
    private void UpdateShaderParameters()
    {
        if (_shaderMaterial == null) return;
        
        _shaderMaterial.SetShaderParameter("shape_color", ShapeColor);
        _shaderMaterial.SetShaderParameter("background_color", BackgroundColor);
        _shaderMaterial.SetShaderParameter("thickness", Thickness);
        _shaderMaterial.SetShaderParameter("smoothness", Smoothness);
        _shaderMaterial.SetShaderParameter("shape_type", ShapeType);
        
        // Triangle-specific parameters
        _shaderMaterial.SetShaderParameter("triangle_p1", TrianglePoint1);
        _shaderMaterial.SetShaderParameter("triangle_p2", TrianglePoint2);
        _shaderMaterial.SetShaderParameter("triangle_p3", TrianglePoint3);
    }
    
    // Optional: Method to change shape at runtime
    public void SetShape(int newShapeType)
    {
        ShapeType = Mathf.Clamp(newShapeType, 0, 4);
        _shaderMaterial?.SetShaderParameter("shape_type", ShapeType);
    }
    
    // Optional: Method to update triangle points at runtime
    public void SetTrianglePoints(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        TrianglePoint1 = p1;
        TrianglePoint2 = p2;
        TrianglePoint3 = p3;
        
        if (_shaderMaterial != null)
        {
            _shaderMaterial.SetShaderParameter("triangle_p1", TrianglePoint1);
            _shaderMaterial.SetShaderParameter("triangle_p2", TrianglePoint2);
            _shaderMaterial.SetShaderParameter("triangle_p3", TrianglePoint3);
        }
    }
    
    // Optional: Animate the shape
    public override void _Process(double delta)
    {
        // Example: Rotate the plane
        // RotateY((float)(delta * 0.5));
        
        // Example: Animate thickness
        // thickness = (float)(Mathf.Sin(Time.GetUnixTimeFromSystem()) * 0.1 + 0.15);
        // shaderMaterial?.SetShaderParameter("thickness", thickness);
    }
}