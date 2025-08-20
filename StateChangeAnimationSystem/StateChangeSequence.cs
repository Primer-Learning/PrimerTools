using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools;

public abstract partial class StateChangeSequence : Node
{
    private struct FlattenedAnimation
    {
        public IAnimatedStateChange Animation { get; }
        public double AbsoluteStartTime { get; }
        public double AbsoluteEndTime => AbsoluteStartTime + Animation.Duration;
        
        public FlattenedAnimation(IAnimatedStateChange animation, double absoluteStartTime)
        {
            Animation = animation;
            AbsoluteStartTime = absoluteStartTime;
        }
    }
    
    [Export] private double _startFromTime = 0;
    public double StartFromTime => _startFromTime;
    
    private readonly CompositeStateChange _rootComposite = new();
    private List<FlattenedAnimation> _flattenedAnimations;
    private double _timeAccumulator = 0;
    private double _currentTime = 0;
    private bool _isPlaying = false;

    public double LastStateChangeTime => _rootComposite.CurrentEndTime;
    
    public bool IsPlaying => _isPlaying;
    public double CurrentTime => _currentTime;
    public double TotalDuration => _rootComposite.Duration;
    
    [Export] private double _playbackSpeed = 1.0;
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set => _playbackSpeed = Mathf.Max(0.0, value);
    }
    
    public override void _Ready()
    {
        Define();

        // Flatten the entire animation tree once
        _flattenedAnimations = _rootComposite.Flatten()
            .Select(f => new FlattenedAnimation(f.change, f.absoluteStartTime))
            .OrderBy(f => f.AbsoluteStartTime)
            .ToList();

        // Record all the start states and then revert
        foreach (var animation in _flattenedAnimations)
        {
            animation.Animation.RecordStartState();
            animation.Animation.ApplyEndState();
        }
        
        // Revert in reverse order
        for (var i = _flattenedAnimations.Count - 1; i >= 0; i--)
        {
            _flattenedAnimations[i].Animation.Revert();
        }

        if (_startFromTime < 0) _startFromTime = TotalDuration;
        SeekTo(_startFromTime);
        
        if (_startFromTime < TotalDuration && _playbackSpeed > 0)
        {
            Play();
        }
    }
    
    public override void _Process(double delta)
    {
        if (!_isPlaying) return;
        
        // Update time based on playback speed
        var scaledDelta = delta * _playbackSpeed;
        var newTime = _currentTime + scaledDelta;
        
        // Clamp to total duration
        if (newTime >= TotalDuration)
        {
            newTime = TotalDuration;
            _isPlaying = false;
            
            // Notify recorder if present
            var recorder = GetParent()?.GetChildren().OfType<SceneRecorder>().FirstOrDefault();
            recorder?.OnSequenceComplete();
        }
        
        // Use the existing seek logic for frame-by-frame updates
        SeekToInternal(newTime);
        _currentTime = newTime;
    }
    
    protected abstract void Define();
    
    private void SeekToInternal(double time)
    {
        if (time < 0)
        {
            GD.Print(TotalDuration);
            time = TotalDuration;
        }
        
        // Revert animations that haven't started yet
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteStartTime > time).Reverse())
        {
            animation.Animation.Revert();
        }
        
        // Apply all animations that should be complete by this time
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteEndTime <= time))
        {
            animation.Animation.ApplyEndState();
            if (animation.Animation is MethodTriggerStateChange methodTrigger)
            {
                if (_isPlaying) methodTrigger.Execute();
                else methodTrigger.SetTriggered(); 
            }
        }
        
        // Evaluate animations that we're in the middle of
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteStartTime <= time && a.AbsoluteEndTime > time))
        {
            var elapsedTime = time - animation.AbsoluteStartTime;
            animation.Animation.EvaluateAtTime(elapsedTime);
        }
    }
    
    public void SeekTo(double time)
    {
        SeekToInternal(time);
        _currentTime = time;
        _startFromTime = time;
        _timeAccumulator = time - _startFromTime;
    }
    // Add pause/resume methods
    public void Pause()
    {
        _isPlaying = false;
    }

    public void Resume()
    {
        _isPlaying = true;
    }
    
    private void Play()
    {
        _isPlaying = true;
        _timeAccumulator = 0;
        // No more tween creation - just set the flag
    }
    
    
    public void AddStateChange(IStateChange stateChange, double delay = 0)
    {
        _rootComposite.AddStateChange(stateChange, delay);
        // GD.Print($"Added sequential: {stateChange.Name} (delay: {delay}s)");
    }
    
    public void AddStateChangeAt(IStateChange stateChange, double absoluteTime)
    {
        _rootComposite.AddStateChangeAt(stateChange, absoluteTime);
        // GD.Print($"Added at {absoluteTime}s: {stateChange.Name}");
    }
    
    public void AddStateChangeInParallel(IStateChange stateChange, double delay = 0)
    {
        _rootComposite.AddStateChangeInParallel(stateChange, delay);
        // GD.Print($"Added in parallel: {stateChange.Name}");
    }
}
