using System;
using Godot;

namespace PrimerTools.TweenSystem;

public class PropertyStateChange : IAnimatedStateChange
{
    private GodotObject _target;
    private string _property;
    private Variant _endValue;
    private double _duration;
    private string _customName;
    
    // Only needs to be a class field for reverting
    private Variant _startValue;
    private bool _startValueSet;
    private Variant StartValue
    {
        get => _startValue;
        set
        {
            if (_startValueSet) return;
            // GD.Print($"Setting _startValue of {Name} to {value}");
            _startValue = value;
            _startValueSet = true;
        }
    }
    
    // TODO: Make custom easing somehow.
    // It should work to use TweenMethod with a method that takes the easing function and applies Tween.InterpolateValue
    private Tween.TransitionType _transition;
    private Tween.EaseType _ease;

    public string Name => _customName ??
                          (_target is Node targetNode
                              ? $"{targetNode.Name}.{_property} -> {_endValue}"
                              : $"{_target}.{_property} -> {_endValue}"
                          );
    public double Duration => _duration;
    
    // Analogous to AnimationUtilities.AnimateValue
    public PropertyStateChange(GodotObject target, string property, Variant endValue)
    {
        _target = target;
        _property = property;
        _endValue = endValue;
        _duration = Node3DStateChangeExtensions.DefaultDuration;
        _transition = Tween.TransitionType.Cubic;
        _ease = Tween.EaseType.InOut;
    }

    public void EvaluateAtTime(double elapsedTime)
    {
        if (elapsedTime >= _duration)
        {
            ApplyEndState();
            return;
        }
        
        if (elapsedTime <= 0)
        {
            _target.Set(_property, StartValue);
            return;
        }
        
        // Interpolate to find current value
        var currentValue = InterpolateValue(StartValue, _endValue, elapsedTime, _duration);
        _target.Set(_property, currentValue);
    }
    
    public void ApplyEndState()
    {
        // Do the application
        // GD.Print($"Applying eng state of {Name}, which is {_endValue}");
        _target.Set(_property, _endValue);
    }

    public void RecordStartState()
    {
        StartValue = _target.Get(_property);
    }
    public void Revert()
    {
        // GD.Print($"StartValue of {Name} is {StartValue}");
        _target.Set(_property, StartValue);
    }

    // Fluent API for configuration
    public PropertyStateChange WithDuration(double duration)
    {
        _duration = duration;
        return this;
    }
    
    IStateChange IStateChange.WithDuration(double duration)
    {
        return WithDuration(duration);
    }
    
    public PropertyStateChange WithTransition(Tween.TransitionType transition)
    {
        _transition = transition;
        return this;
    }
    
    public PropertyStateChange WithEase(Tween.EaseType ease)
    {
        _ease = ease;
        return this;
    }
    
    public PropertyStateChange WithName(string name)
    {
        _customName = name;
        return this;
    }
    
    // For some reason, Tween.InterpolateValue takes a diff instead of final, but Variants don't have subtraction defined.
    // So we make our own interpolation thing, I guess.
    private Variant InterpolateValue(Variant start, Variant end, double elapsedTime, double duration)
    {
        return start.VariantType switch
        {
            Variant.Type.Float => Tween.InterpolateValue(start.AsDouble(), _endValue.AsDouble() - start.AsDouble(), elapsedTime, duration, _transition, _ease),
            Variant.Type.Vector2 => Tween.InterpolateValue(start.AsVector2(), _endValue.AsVector2() - start.AsVector2(), elapsedTime, duration, _transition, _ease),
            Variant.Type.Vector3 => Tween.InterpolateValue(start.AsVector3(), _endValue.AsVector3() - start.AsVector3(), elapsedTime, duration, _transition, _ease),
            Variant.Type.Color => Tween.InterpolateValue(start.AsColor(), _endValue.AsColor() - start.AsColor(), elapsedTime, duration, _transition, _ease),
            Variant.Type.Quaternion => start.AsQuaternion().Slerp(_endValue.AsQuaternion(), (float)(elapsedTime / duration)), // _transition, _ease // No transition or ease here for now
            _ => end // For non-interpolatable types, just use end value
        };
    }
}
