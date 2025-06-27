using Godot;
using System;

namespace PrimerTools._2D.Diagram;

public abstract partial class ShapeData : GodotObject
{
    public event Action ShapeChanged;
    
    public abstract int GetShapeType();
    public abstract Rect2 GetBounds(float padding = 0);
    public abstract void SetShaderParameters(ShaderMaterial material, string prefix = "");
    
    protected void NotifyChanged() => ShapeChanged?.Invoke();
}

public partial class CircleData : ShapeData
{
    private Vector2 _center;
    public Vector2 Center 
    { 
        get => _center;
        set
        {
            _center = value;
            NotifyChanged();
        }
    }
    
    private float _radius;
    public float Radius 
    { 
        get => _radius;
        set
        {
            _radius = value;
            NotifyChanged();
        }
    }

    public CircleData(Vector2 center, float radius)
    {
        _center = center;
        _radius = radius;
    }

    public CircleData()
    {
    }

    public override int GetShapeType() => 0;

    public override Rect2 GetBounds(float padding = 0)
    {
        return new Rect2(_center - Vector2.One * (_radius + padding), Vector2.One * (_radius + padding) * 2);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}shape_center", _center);
        material.SetShaderParameter($"{prefix}radius", _radius);
    }
}

public partial class RectangleData : ShapeData
{
    private Vector2 _center;
    public Vector2 Center 
    { 
        get => _center;
        set
        {
            _center = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _size;
    public Vector2 Size 
    { 
        get => _size;
        set
        {
            _size = value;
            NotifyChanged();
        }
    }

    public RectangleData(Vector2 center, Vector2 size)
    {
        _center = center;
        _size = size;
    }

    public RectangleData()
    {
    }

    public override int GetShapeType() => 1;

    public override Rect2 GetBounds(float padding = 0)
    {
        return new Rect2(_center - _size - Vector2.One * padding, (_size + Vector2.One * padding) * 2);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}shape_center", _center);
        material.SetShaderParameter($"{prefix}size", _size);
    }
}

public partial class LineData : ShapeData
{
    private Vector2 _pointA;
    public Vector2 PointA 
    { 
        get => _pointA;
        set
        {
            _pointA = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _pointB;
    public Vector2 PointB 
    { 
        get => _pointB;
        set
        {
            _pointB = value;
            NotifyChanged();
        }
    }

    public LineData(Vector2 pointA, Vector2 pointB)
    {
        _pointA = pointA;
        _pointB = pointB;
    }

    public LineData()
    {
    }

    public override int GetShapeType() => 2;

    public override Rect2 GetBounds(float padding = 0)
    {
        var minX = Mathf.Min(_pointA.X, _pointB.X) - padding;
        var minY = Mathf.Min(_pointA.Y, _pointB.Y) - padding;
        var maxX = Mathf.Max(_pointA.X, _pointB.X) + padding;
        var maxY = Mathf.Max(_pointA.Y, _pointB.Y) + padding;
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}point_a", _pointA);
        material.SetShaderParameter($"{prefix}point_b", _pointB);
    }
}

public partial class TriangleData : ShapeData
{
    private Vector2 _pointA;
    public Vector2 PointA 
    { 
        get => _pointA;
        set
        {
            _pointA = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _pointB;
    public Vector2 PointB 
    { 
        get => _pointB;
        set
        {
            _pointB = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _pointC;
    public Vector2 PointC 
    { 
        get => _pointC;
        set
        {
            _pointC = value;
            NotifyChanged();
        }
    }

    public TriangleData(Vector2 pointA, Vector2 pointB, Vector2 pointC)
    {
        _pointA = pointA;
        _pointB = pointB;
        _pointC = pointC;
    }

    public TriangleData()
    {
    }

    public override int GetShapeType() => 3;

    public override Rect2 GetBounds(float padding = 0)
    {
        var minX = Mathf.Min(Mathf.Min(_pointA.X, _pointB.X), _pointC.X) - padding;
        var minY = Mathf.Min(Mathf.Min(_pointA.Y, _pointB.Y), _pointC.Y) - padding;
        var maxX = Mathf.Max(Mathf.Max(_pointA.X, _pointB.X), _pointC.X) + padding;
        var maxY = Mathf.Max(Mathf.Max(_pointA.Y, _pointB.Y), _pointC.Y) + padding;
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}point_a", _pointA);
        material.SetShaderParameter($"{prefix}point_b", _pointB);
        material.SetShaderParameter($"{prefix}point_c", _pointC);
    }
}

public enum SdfOperation
{
    Union,
    SmoothUnion,
    Subtraction,
    SmoothSubtraction,
    Intersection,
    SmoothIntersection
}

public partial class CompositeShapeData : ShapeData
{
    private ShapeData _shape1;
    public ShapeData Shape1 
    { 
        get => _shape1;
        set
        {
            if (_shape1 != null)
                _shape1.ShapeChanged -= OnChildShapeChanged;
            
            _shape1 = value;
            
            if (_shape1 != null)
                _shape1.ShapeChanged += OnChildShapeChanged;
            
            NotifyChanged();
        }
    }
    
    private ShapeData _shape2;
    public ShapeData Shape2 
    { 
        get => _shape2;
        set
        {
            if (_shape2 != null)
                _shape2.ShapeChanged -= OnChildShapeChanged;
            
            _shape2 = value;
            
            if (_shape2 != null)
                _shape2.ShapeChanged += OnChildShapeChanged;
            
            NotifyChanged();
        }
    }
    
    private SdfOperation _operation = SdfOperation.Union;
    public SdfOperation Operation 
    { 
        get => _operation;
        set
        {
            _operation = value;
            NotifyChanged();
        }
    }
    
    private float _smoothness = 0.1f;
    public float Smoothness 
    { 
        get => _smoothness;
        set
        {
            _smoothness = value;
            NotifyChanged();
        }
    }

    public CompositeShapeData(ShapeData shape1, ShapeData shape2, SdfOperation operation = SdfOperation.Union, float smoothness = 0.1f)
    {
        Shape1 = shape1;
        Shape2 = shape2;
        _operation = operation;
        _smoothness = smoothness;
    }

    public CompositeShapeData()
    {
    }

    private void OnChildShapeChanged()
    {
        NotifyChanged();
    }

    public override int GetShapeType() => 99; // Special type for composite

    public override Rect2 GetBounds(float padding = 0)
    {
        if (_shape1 == null || _shape2 == null)
            return new Rect2();
        
        var bounds1 = _shape1.GetBounds(0);
        var bounds2 = _shape2.GetBounds(0);
        
        // Union the bounds for most operations
        // For subtraction/intersection, we might want just bounds1, but union is safer
        var combined = bounds1.Merge(bounds2);
        
        // Grow by padding
        return combined.Grow(padding);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        if (_shape1 == null || _shape2 == null)
            return;
        
        // Set operation parameters
        material.SetShaderParameter($"{prefix}operation", (int)_operation);
        material.SetShaderParameter($"{prefix}operation_smoothness", _smoothness);
        
        // Set shape types
        material.SetShaderParameter($"{prefix}shape1_type", _shape1.GetShapeType());
        material.SetShaderParameter($"{prefix}shape2_type", _shape2.GetShapeType());
        
        // Set shape-specific parameters
        _shape1.SetShaderParameters(material, $"{prefix}shape1_");
        _shape2.SetShaderParameters(material, $"{prefix}shape2_");
    }
}
