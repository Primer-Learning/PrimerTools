using Godot;
using System;

namespace PrimerTools._2D.Diagram;

public partial class ShapeStyle : GodotObject
{
    private float _thickness = 0.1f;
    private float _innerThickness = 0.01f;
    private Color _shapeColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    
    public float Thickness 
    { 
        get => _thickness;
        set 
        { 
            _thickness = value; 
            StyleChanged?.Invoke(); 
        }
    }
    
    public float InnerThickness 
    { 
        get => _innerThickness;
        set 
        { 
            _innerThickness = value; 
            StyleChanged?.Invoke(); 
        }
    }
    
    public Color ShapeColor
    {
        get => _shapeColor;
        set
        {
            _shapeColor = value;
            StyleChanged?.Invoke();
        }
    }
    
    public event Action StyleChanged;
    
    public ShapeStyle() { }
    
    public ShapeStyle(float thickness, float innerThickness, Color shapeColor)
    {
        _thickness = thickness;
        _innerThickness = innerThickness;
        _shapeColor = shapeColor;
    }
    
    public void ApplyToShader(ShaderMaterial material)
    {
        material.SetShaderParameter("thickness", _thickness);
        material.SetShaderParameter("inner_thickness", _innerThickness);
        material.SetShaderParameter("shape_color", _shapeColor);
    }
    
    public ShapeStyle Clone()
    {
        return new ShapeStyle(_thickness, _innerThickness, _shapeColor);
    }
}
