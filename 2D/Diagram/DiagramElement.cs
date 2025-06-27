using Godot;

namespace PrimerTools._2D.Diagram;

public abstract partial class DiagramElement : Node
{
    protected MeshInstance3D _meshInstance;
    protected ShaderMaterial _shaderMaterial;
    
    private Vector2 _position;
    public Vector2 Position 
    { 
        get => _position;
        set
        {
            _position = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }
    
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

    protected DiagramElement(Vector2 position, float padding = 1)
    {
        _position = position;
        _padding = padding;
    }

    public abstract int GetShapeType();
    public abstract Rect2 GetBounds();
    
    protected virtual string GetShaderPath()
    {
        // Default implementation - derived classes should override
        return "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader";
    }
    
    public virtual void CreateMesh(DiagramSystem parentSystem)
    {
        // Create mesh instance
        _meshInstance = new MeshInstance3D();
        _meshInstance.RotationDegrees = new Vector3(90, 0, 0);
        parentSystem.AddChild(_meshInstance);
        _meshInstance.Name = $"DiagramElement_{GetType().Name}";
        
        // Create plane mesh
        var bounds = GetBounds();
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
    
    protected virtual void UpdateMeshTransform()
    {
        if (_meshInstance == null) return;
        
        var bounds = GetBounds();
        _meshInstance.Position = new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y, 0);
        
        // Update mesh size if needed
        if (_meshInstance.Mesh is PlaneMesh planeMesh)
        {
            planeMesh.Size = bounds.Size;
        }
    }
    
    protected abstract void UpdateShaderParameters();
}

public partial class CircleElement : DiagramElement
{
    private float _radius;
    public float Radius 
    { 
        get => _radius;
        set
        {
            _radius = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }
    
    private Vector2 _center;
    public Vector2 Center 
    { 
        get => _center;
        set
        {
            _center = value;
            Position = value; // Update position to match center
            UpdateShaderParameters();
        }
    }

    public CircleElement(Vector2 center, float radius, float padding = 1) : base(center, padding)
    {
        _radius = radius;
        _center = center;
        Name = "CircleElement";
    }

    protected override string GetShaderPath()
    {
        return "res://addons/PrimerTools/2D/Diagram/ShapeShaders/circle_shader.gdshader";
    }

    public override int GetShapeType() => 0; // Circle type in shader

    public override Rect2 GetBounds()
    {
        // Return bounding box for the circle
        return new Rect2(Position - Vector2.One * (Radius + Padding), Vector2.One * (Radius + Padding) * 2);
    }
    
    protected override void UpdateShaderParameters()
    {
        if (_shaderMaterial == null) return;
        
        _shaderMaterial.SetShaderParameter("shape_center", Center);
        _shaderMaterial.SetShaderParameter("radius", Radius);
    }
}

public partial class RectangleElement : DiagramElement
{
    private Vector2 _size;
    public Vector2 Size 
    { 
        get => _size;
        set
        {
            _size = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }
    
    private Vector2 _center;
    public Vector2 Center 
    { 
        get => _center;
        set
        {
            _center = value;
            Position = value; // Update position to match center
            UpdateShaderParameters();
        }
    }

    public RectangleElement(Vector2 center, Vector2 size, float padding = 1) : base(center, padding)
    {
        _size = size;
        _center = center;
        Name = "RectangleElement";
    }

    protected override string GetShaderPath()
    {
        return "res://addons/PrimerTools/2D/Diagram/ShapeShaders/rectangle_shader.gdshader";
    }

    public override int GetShapeType() => 1; // Rectangle type in shader

    public override Rect2 GetBounds()
    {
        // Return bounding box for the rectangle
        return new Rect2(Position - Size - Vector2.One * Padding, (Size + Vector2.One * Padding) * 2);
    }
    
    protected override void UpdateShaderParameters()
    {
        if (_shaderMaterial == null) return;
        
        _shaderMaterial.SetShaderParameter("shape_center", Center);
        _shaderMaterial.SetShaderParameter("size", Size);
    }
}

public partial class LineElement : DiagramElement
{
    private Vector2 _pointA;
    public Vector2 PointA 
    { 
        get => _pointA;
        set
        {
            _pointA = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }
    
    private Vector2 _pointB;
    public Vector2 PointB 
    { 
        get => _pointB;
        set
        {
            _pointB = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }

    public LineElement(Vector2 pointA, Vector2 pointB, float padding = 1) : base((pointA + pointB) / 2, padding)
    {
        _pointA = pointA;
        _pointB = pointB;
        Name = "LineElement";
    }

    protected override string GetShaderPath()
    {
        return "res://addons/PrimerTools/2D/Diagram/ShapeShaders/line_shader.gdshader";
    }

    public override int GetShapeType() => 2; // Line type in shader

    public override Rect2 GetBounds()
    {
        // Calculate bounding box that contains both points
        var minX = Mathf.Min(_pointA.X, _pointB.X) - Padding;
        var minY = Mathf.Min(_pointA.Y, _pointB.Y) - Padding;
        var maxX = Mathf.Max(_pointA.X, _pointB.X) + Padding;
        var maxY = Mathf.Max(_pointA.Y, _pointB.Y) + Padding;
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    protected override void UpdateShaderParameters()
    {
        if (_shaderMaterial == null) return;
        
        _shaderMaterial.SetShaderParameter("point_a", _pointA);
        _shaderMaterial.SetShaderParameter("point_b", _pointB);
    }
}

public partial class TriangleElement : DiagramElement
{
    private Vector2 _pointA;
    public Vector2 PointA 
    { 
        get => _pointA;
        set
        {
            _pointA = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }
    
    private Vector2 _pointB;
    public Vector2 PointB 
    { 
        get => _pointB;
        set
        {
            _pointB = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }
    
    private Vector2 _pointC;
    public Vector2 PointC 
    { 
        get => _pointC;
        set
        {
            _pointC = value;
            UpdateMeshTransform();
            UpdateShaderParameters();
        }
    }

    public TriangleElement(Vector2 pointA, Vector2 pointB, Vector2 pointC, float padding = 1) 
        : base((pointA + pointB + pointC) / 3, padding)
    {
        _pointA = pointA;
        _pointB = pointB;
        _pointC = pointC;
        Name = "TriangleElement";
    }

    protected override string GetShaderPath()
    {
        return "res://addons/PrimerTools/2D/Diagram/ShapeShaders/triangle_shader.gdshader";
    }

    public override int GetShapeType() => 3; // Triangle type in shader

    public override Rect2 GetBounds()
    {
        // Calculate bounding box that contains all three points
        var minX = Mathf.Min(Mathf.Min(_pointA.X, _pointB.X), _pointC.X) - Padding;
        var minY = Mathf.Min(Mathf.Min(_pointA.Y, _pointB.Y), _pointC.Y) - Padding;
        var maxX = Mathf.Max(Mathf.Max(_pointA.X, _pointB.X), _pointC.X) + Padding;
        var maxY = Mathf.Max(Mathf.Max(_pointA.Y, _pointB.Y), _pointC.Y) + Padding;
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    protected override void UpdateShaderParameters()
    {
        if (_shaderMaterial == null) return;
        
        _shaderMaterial.SetShaderParameter("point_a", _pointA);
        _shaderMaterial.SetShaderParameter("point_b", _pointB);
        _shaderMaterial.SetShaderParameter("point_c", _pointC);
    }
}
