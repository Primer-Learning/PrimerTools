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
