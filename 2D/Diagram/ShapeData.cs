using Godot;
using System;

namespace PrimerTools._2D.Diagram;

public abstract partial class ShapeData : GodotObject
{
    public event Action ShapeChanged;
    
    public abstract int GetShapeType();
    public abstract Rect2 GetBounds();
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

    public override Rect2 GetBounds()
    {
        return new Rect2(_center - Vector2.One * _radius, Vector2.One * _radius * 2);
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

    public override Rect2 GetBounds()
    {
        return new Rect2(_center - _size, _size * 2);
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

    public override Rect2 GetBounds()
    {
        var minX = Mathf.Min(_pointA.X, _pointB.X);
        var minY = Mathf.Min(_pointA.Y, _pointB.Y);
        var maxX = Mathf.Max(_pointA.X, _pointB.X);
        var maxY = Mathf.Max(_pointA.Y, _pointB.Y);
        
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

    public override Rect2 GetBounds()
    {
        var minX = Mathf.Min(Mathf.Min(_pointA.X, _pointB.X), _pointC.X);
        var minY = Mathf.Min(Mathf.Min(_pointA.Y, _pointB.Y), _pointC.Y);
        var maxX = Mathf.Max(Mathf.Max(_pointA.X, _pointB.X), _pointC.X);
        var maxY = Mathf.Max(Mathf.Max(_pointA.Y, _pointB.Y), _pointC.Y);
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}point_a", _pointA);
        material.SetShaderParameter($"{prefix}point_b", _pointB);
        material.SetShaderParameter($"{prefix}point_c", _pointC);
    }
}

public partial class ArrowData : ShapeData
{
    private Vector2 _start;
    public Vector2 Start 
    { 
        get => _start;
        set
        {
            _start = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _end;
    public Vector2 End 
    { 
        get => _end;
        set
        {
            _end = value;
            NotifyChanged();
        }
    }
    
    private float _headLength = 10.0f;
    public float HeadLength 
    { 
        get => _headLength;
        set
        {
            _headLength = value;
            NotifyChanged();
        }
    }
    
    private float _headAngle = 30.0f; // Angle in degrees
    public float HeadAngle 
    { 
        get => _headAngle;
        set
        {
            _headAngle = value;
            NotifyChanged();
        }
    }

    public ArrowData(Vector2 start, Vector2 end, float headLength = 3.0f, float headAngle = 35.0f)
    {
        _start = start;
        _end = end;
        _headLength = headLength;
        _headAngle = headAngle;
    }

    public ArrowData()
    {
    }

    public override int GetShapeType() => 4;

    public override Rect2 GetBounds()
    {
        // Calculate bounds including the arrowhead
        var direction = (_end - _start).Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var angleRad = Mathf.DegToRad(_headAngle);
        var headWidth = _headLength * Mathf.Tan(angleRad);
        
        // Points to consider for bounds: start, end, and the two arrowhead wing tips
        var wing1 = _end - direction * _headLength + perpendicular * headWidth;
        var wing2 = _end - direction * _headLength - perpendicular * headWidth;
        
        var minX = Mathf.Min(Mathf.Min(Mathf.Min(_start.X, _end.X), wing1.X), wing2.X);
        var minY = Mathf.Min(Mathf.Min(Mathf.Min(_start.Y, _end.Y), wing1.Y), wing2.Y);
        var maxX = Mathf.Max(Mathf.Max(Mathf.Max(_start.X, _end.X), wing1.X), wing2.X);
        var maxY = Mathf.Max(Mathf.Max(Mathf.Max(_start.Y, _end.Y), wing1.Y), wing2.Y);
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}arrow_start", _start);
        material.SetShaderParameter($"{prefix}arrow_end", _end);
        material.SetShaderParameter($"{prefix}head_length", _headLength);
        material.SetShaderParameter($"{prefix}head_angle", _headAngle);
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

    public CompositeShapeData() {}

    private void OnChildShapeChanged()
    {
        NotifyChanged();
    }

    public override int GetShapeType() => 99; // Special type for composite

    public override Rect2 GetBounds()
    {
        if (_shape1 == null || _shape2 == null)
            return new Rect2();
        
        var bounds1 = _shape1.GetBounds();
        var bounds2 = _shape2.GetBounds();
        
        // Union the bounds for most operations
        // For subtraction/intersection, we might want just bounds1, but union is safer
        return bounds1.Merge(bounds2);
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

public partial class BracketData : ShapeData
{
    private Vector2 _leftTip;
    public Vector2 LeftTip 
    { 
        get => _leftTip;
        set
        {
            _leftTip = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _rightTip;
    public Vector2 RightTip 
    { 
        get => _rightTip;
        set
        {
            _rightTip = value;
            NotifyChanged();
        }
    }
    
    private Vector2 _stem;
    public Vector2 Stem 
    { 
        get => _stem;
        set
        {
            _stem = value;
            NotifyChanged();
        }
    }

    public BracketData(Vector2 leftTip, Vector2 rightTip, Vector2 stem)
    {
        _leftTip = leftTip;
        _rightTip = rightTip;
        _stem = stem;
    }

    public BracketData()
    {
        _leftTip = new Vector2(-0.5f, -1.0f);
        _rightTip = new Vector2(0.5f, -1.0f);
        _stem = Vector2.Zero;
    }

    public override int GetShapeType() => 5; // New type for bracket

    public override Rect2 GetBounds()
    {
        // Calculate bounds that encompass all three points
        var minX = Mathf.Min(Mathf.Min(_leftTip.X, _rightTip.X), _stem.X);
        var minY = Mathf.Min(Mathf.Min(_leftTip.Y, _rightTip.Y), _stem.Y);
        var maxX = Mathf.Max(Mathf.Max(_leftTip.X, _rightTip.X), _stem.X);
        var maxY = Mathf.Max(Mathf.Max(_leftTip.Y, _rightTip.Y), _stem.Y);
        
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
    
    public override void SetShaderParameters(ShaderMaterial material, string prefix = "")
    {
        material.SetShaderParameter($"{prefix}bracket_tip1", _leftTip);
        material.SetShaderParameter($"{prefix}bracket_tip2", _rightTip);
        material.SetShaderParameter($"{prefix}bracket_stem", _stem);
    }
}
