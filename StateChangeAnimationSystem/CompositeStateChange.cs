using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools;
using PrimerTools.TweenSystem;

public class CompositeStateChange : IStateChange
{
    private const float TimeEpsilon = 0.001f;
    
    private readonly List<TimedStateChange> _timedChanges = new();
    
    private struct TimedStateChange
    {
        public IStateChange StateChange { get; }
        public double StartTime { get; }
        public double EndTime => StartTime + StateChange.Duration;
        
        public TimedStateChange(IStateChange stateChange, double startTime)
        {
            StateChange = stateChange;
            StartTime = startTime;
        }
    }
    private double _currentEndTime = 0;
    public double CurrentEndTime => _currentEndTime;
    private string _customName;
    
    public string Name => _customName ?? (_timedChanges.Count > 0 
        ? $"Composite starting with {_timedChanges[0].StateChange.Name}"
        : "Empty Composite");
        
    public double Duration => _timedChanges.Count > 0 
        ? _timedChanges.Max(tc => tc.EndTime) 
        : 0;

    public CompositeStateChange(){}
    
    public static CompositeStateChange Series(params IStateChange[] stateChanges)
    {
        var composite = new CompositeStateChange();
        foreach (var change in stateChanges)
        {
            composite.AddStateChange(change);
        }
    
        return composite;
    }
    public static CompositeStateChange Parallel(params IStateChange[] stateChanges)
    {
        var composite = new CompositeStateChange();
        foreach (var change in stateChanges)
        {
            composite.AddStateChangeInParallel(change);
        }
    
        return composite;
    }
    
    // Sequential add - starts after the last registered change
    public void AddStateChange(IStateChange change, double delay = 0)
    {
        var startTime = _currentEndTime + delay;
        _timedChanges.Add(new TimedStateChange(change, startTime));
        _currentEndTime = startTime + change.Duration;
    }
    
    // Sequential add with delay in minutes and seconds
    public void AddStateChange(IStateChange change, int delayMinutes, float delaySeconds)
    {
        AddStateChange(change, delayMinutes * 60.0 + delaySeconds);
    }
    
    // Sequential add with delay in minutes, seconds, and frames (60ths of a second)
    public void AddStateChange(IStateChange change, int delayMinutes, int delaySeconds, int delayFrames)
    {
        AddStateChange(change, delayMinutes * 60.0 + delaySeconds + delayFrames / 60.0);
    }
    
    // Add at absolute time relative to this composite's start
    public void AddStateChangeAt(IStateChange change, double absoluteTime)
    {
        _timedChanges.Add(new TimedStateChange(change, absoluteTime));
        _currentEndTime = Math.Max(_currentEndTime, absoluteTime + change.Duration);
    }
    
    // Add at absolute time specified in minutes and seconds
    public void AddStateChangeAt(IStateChange change, int minutes, float seconds)
    {
        AddStateChangeAt(change, minutes * 60.0 + seconds);
    }
    
    // Add at absolute time specified in minutes, seconds, and frames (60ths of a second)
    public void AddStateChangeAt(IStateChange change, int minutes, int seconds, int frames)
    {
        AddStateChangeAt(change, minutes * 60.0 + seconds + frames / 60.0);
    }
    
    // Add in parallel with the last change
    // Convenient way of making it start relative to the start of the previous one without having to know the time.
    public void AddStateChangeInParallel(IStateChange change, double delay = 0)
    {
        var startTime = _timedChanges.Count > 0 ? _timedChanges.Last().StartTime : 0;
        startTime += delay;
        _timedChanges.Add(new TimedStateChange(change, startTime));
        _currentEndTime = Math.Max(_currentEndTime, startTime + change.Duration);
    }
    
    // Add in parallel with delay in minutes and seconds
    public void AddStateChangeInParallel(IStateChange change, int delayMinutes, float delaySeconds)
    {
        AddStateChangeInParallel(change, delayMinutes * 60.0 + delaySeconds);
    }
    
    // Add in parallel with delay in minutes, seconds, and frames (60ths of a second)
    public void AddStateChangeInParallel(IStateChange change, int delayMinutes, int delaySeconds, int delayFrames)
    {
        AddStateChangeInParallel(change, delayMinutes * 60.0 + delaySeconds + delayFrames / 60.0);
    }
    
    public CompositeStateChange WithName(string name)
    {
        _customName = name;
        return this;
    }
    
    public CompositeStateChange WithDuration(double newDuration)
    {
        newDuration = Mathf.Max(TimeEpsilon, newDuration);
        
        var currentDuration = Duration;
        if (currentDuration <= 0) return this; // Can't scale zero duration
        
        var scaleFactor = newDuration / currentDuration;
        
        // Create new list with scaled times
        var scaledChanges = new List<TimedStateChange>();
        foreach (var timedChange in _timedChanges)
        {
            var scaledChange = new TimedStateChange(
                timedChange.StateChange.WithDuration(timedChange.StateChange.Duration * scaleFactor),
                timedChange.StartTime * scaleFactor
            );
            scaledChanges.Add(scaledChange);
        }
        
        _timedChanges.Clear();
        _timedChanges.AddRange(scaledChanges);
        _currentEndTime *= scaleFactor;
        
        return this;
    }
    
    IStateChange IStateChange.WithDuration(double duration)
    {
        return WithDuration(duration);
    }
    
    public IEnumerable<(IAnimatedStateChange change, double absoluteStartTime)> Flatten(double baseTime = 0)
    {
        foreach (var timedChange in _timedChanges)
        {
            if (timedChange.StateChange is CompositeStateChange nested)
            {
                // Recursively flatten nested composites
                foreach (var flat in nested.Flatten(baseTime + timedChange.StartTime))
                {
                    yield return flat;
                }
            }
            else if (timedChange.StateChange is IAnimatedStateChange animatedChange)
            {
                // Return leaf animations with absolute time
                yield return (animatedChange, baseTime + timedChange.StartTime);
            }
        }
    }
    
}
