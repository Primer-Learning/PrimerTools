using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface IAnimationCommand
{
    double StartTime { get; }
    double EndTime { get; }
    void Execute(double fromTime = 0);
    void ApplyEndState();
    // Revert might be added later for back-seeking
}

public abstract partial class TweenSequence : Node
{
    [Export] private double _startFromTime = 0;
    
    private List<IAnimationCommand> _commands = new();
    private double _currentTime = 0;
    private bool _isPlaying = false;
    
    public override void _Ready()
    {
        Define();
        
        if (_startFromTime > 0)
        {
            SeekTo(_startFromTime);
        }
        
        Play();
    }
    
    protected abstract void Define();
    
    public void SeekTo(double time)
    {
        foreach (var processedTween in GetTree().GetProcessedTweens())
        {
            processedTween.Kill();
        }
        
        // Apply all commands that should be complete by this time
        foreach (var command in _commands.Where(c => c.EndTime <= time))
        {
            command.ApplyEndState();
        }
        
        // Execute commands that should be in progress
        foreach (var command in _commands.Where(c => c.StartTime <= time && c.EndTime > time))
        {
            command.Execute(time);
        }
        
        _currentTime = time;
    }
    
    public void Play()
    {
        _isPlaying = true;
        GD.Print("Playing");
        
        // Schedule future commands
        foreach (var command in _commands.Where(c => c.StartTime >= _currentTime))
        {
            GD.Print("Command");
            var delay = command.StartTime - _currentTime;
            GetTree().CreateTimer((float)delay).Timeout += () => command.Execute();
        }
    }
    
    // Registration methods
    public PropertyAnimation AnimateProperty(Node target, string property, Variant endValue, double duration = 0.5)
    {
        var startTime = GetNextAvailableTime();
        var cmd = new PropertyAnimation(target, property, endValue, startTime, duration);
        RegisterCommand(cmd);
        return cmd;
    }
    
    public PropertyAnimation AnimatePropertyAt(double time, Node target, string property, Variant endValue, double duration = 0.5)
    {
        var cmd = new PropertyAnimation(target, property, endValue, time, duration);
        RegisterCommand(cmd);
        return cmd;
    }
    
    private void RegisterCommand(IAnimationCommand command)
    {
        // Warning for out-of-order registration
        if (_commands.Count > 0 && command.StartTime < _commands.Last().StartTime)
        {
            GD.PushWarning($"Command registered with start time {command.StartTime} before previous command at {_commands.Last().StartTime}");
        }
        
        _commands.Add(command);
    }
    
    private double GetNextAvailableTime()
    {
        if (_commands.Count == 0) return 0;
        return _commands.Max(c => c.EndTime);
    }
}

// Basic property animation command
public class PropertyAnimation : IAnimationCommand
{
    private Node _target;
    private string _property;
    private Variant _startValue;
    private Variant _endValue;
    private double _duration;
    private Tween.TransitionType _transition;
    private Tween.EaseType _ease;
    
    public double StartTime { get; private set; }
    public double EndTime => StartTime + _duration;
    
    public PropertyAnimation(Node target, string property, Variant endValue, double startTime, double duration)
    {
        _target = target;
        _property = property;
        _endValue = endValue;
        StartTime = startTime;
        _duration = duration;
        _transition = Tween.TransitionType.Cubic;
        _ease = Tween.EaseType.InOut;
    }
    
    public void Execute(double fromTime = 0)
    {
        var elapsedTime = Math.Max(0, fromTime - StartTime);
        var remainingDuration = _duration - elapsedTime;
        
        if (remainingDuration <= 0)
        {
            ApplyEndState();
            return;
        }
        
        // If starting partway through, interpolate to find current value
        if (elapsedTime > 0)
        {
            var currentValue =
                Tween.InterpolateValue(_startValue, _endValue, elapsedTime, _duration, _transition, _ease);
            _target.Set(_property, currentValue);
        }
        
        var tween = _target.GetTree().CreateTween();
        tween.TweenProperty(_target, _property, _endValue, remainingDuration)
            .SetTrans(_transition)
            .SetEase(_ease);
    }
    
    public void ApplyEndState()
    {
        _target.Set(_property, _endValue);
    }
    
    // Fluent API for configuration
    public PropertyAnimation WithTransition(Tween.TransitionType transition)
    {
        _transition = transition;
        return this;
    }
    
    public PropertyAnimation WithEase(Tween.EaseType ease)
    {
        _ease = ease;
        return this;
    }
}

// Composite commands for Series and Parallel
public class CompositeAnimation : IAnimationCommand
{
    protected List<IAnimationCommand> _children = new();
    
    public virtual double StartTime => _children.Count > 0 ? _children.Min(c => c.StartTime) : 0;
    public virtual double EndTime => _children.Count > 0 ? _children.Max(c => c.EndTime) : 0;
    
    public virtual void Execute(double fromTime = 0)
    {
        foreach (var child in _children)
        {
            if (child.StartTime >= fromTime)
            {
                // Schedule future execution
                var delay = child.StartTime - fromTime;
                if (delay > 0)
                {
                    // You'd need a reference to the scene tree here
                    // Could be passed in constructor or accessed via singleton
                }
                else
                {
                    child.Execute(fromTime);
                }
            }
            else if (child.EndTime > fromTime)
            {
                // Already in progress
                child.Execute(fromTime);
            }
        }
    }
    
    public virtual void ApplyEndState()
    {
        foreach (var child in _children)
        {
            child.ApplyEndState();
        }
    }
}

public class SeriesAnimation : CompositeAnimation
{
    private double _timeOffset = 0;
    
    public void Add(IAnimationCommand command)
    {
        // Shift command timing to run after previous commands
        var shiftedCommand = new TimeShiftedCommand(command, _timeOffset);
        _children.Add(shiftedCommand);
        _timeOffset = shiftedCommand.EndTime;
    }
}

// Helper wrapper to shift timing
public class TimeShiftedCommand : IAnimationCommand
{
    private IAnimationCommand _inner;
    private double _offset;
    
    public double StartTime => _inner.StartTime + _offset;
    public double EndTime => _inner.EndTime + _offset;
    
    public TimeShiftedCommand(IAnimationCommand inner, double offset)
    {
        _inner = inner;
        _offset = offset;
    }
    
    public void Execute(double fromTime = 0)
    {
        _inner.Execute(fromTime - _offset);
    }
    
    public void ApplyEndState()
    {
        _inner.ApplyEndState();
    }
}