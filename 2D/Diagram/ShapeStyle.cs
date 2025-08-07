using Godot;
using System;

namespace PrimerTools._2D.Diagram;

public partial class ShapeStyle : GodotObject
{
    private float _thickness = 0.1f;
    private float _innerThickness = 0.01f;
    private Color _shapeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private Color _backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    private float _smoothness = 2.5f;
    
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
    
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            StyleChanged?.Invoke();
        }
    }
    
    public float Smoothness
    {
        get => _smoothness;
        set
        {
            _smoothness = value;
            StyleChanged?.Invoke();
        }
    }
    
    public event Action StyleChanged;
    
    public ShapeStyle() { }
    
    public ShapeStyle(float thickness, float innerThickness, Color shapeColor, Color backgroundColor, float smoothness)
    {
        _thickness = thickness;
        _innerThickness = innerThickness;
        _shapeColor = shapeColor;
        _backgroundColor = backgroundColor;
        _smoothness = smoothness;
    }
    
    public void ApplyToShader(ShaderMaterial material)
    {
        material.SetShaderParameter("thickness", _thickness);
        material.SetShaderParameter("inner_thickness", _innerThickness);
        material.SetShaderParameter("shape_color", _shapeColor);
        material.SetShaderParameter("background_color", _backgroundColor);
        material.SetShaderParameter("smoothness", _smoothness);
    }
    
    public ShapeStyle Clone()
    {
        return new ShapeStyle(_thickness, _innerThickness, _shapeColor, _backgroundColor, _smoothness);
    }
}
