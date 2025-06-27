using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

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
    
    private readonly CompositeStateChange _rootComposite = new();
    private List<FlattenedAnimation> _flattenedAnimations;
    private double _timeAccumulator = 0;
    private double _currentTime = 0;
    private bool _isPlaying = false;
    private Tween _masterTween;

    public double LastStateChangeTime => _rootComposite.CurrentEndTime;
    
    public bool IsPlaying => _isPlaying;
    public double CurrentTime => _currentTime;
    public double TotalDuration => _rootComposite.Duration;
    
    [Export] private double _playbackSpeed = 1.0;
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set => _playbackSpeed = Mathf.Max(0.1, value);
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
        
        if (_startFromTime > 0)
        {
            SeekTo(_startFromTime);
        }
        
        Play();
    }
    
    public override void _Process(double delta)
    {
        if (_isPlaying && _masterTween != null && _masterTween.IsValid())
        {
            _timeAccumulator += delta;
            _currentTime = Math.Min(_startFromTime + _timeAccumulator, TotalDuration);

            // Stop if we've reached the end
            if (_currentTime >= TotalDuration)
            {
                _currentTime = TotalDuration;
                _isPlaying = false;
            }
        }
    }
    
    protected abstract void Define();
    
    public void SeekTo(double time)
    {
        KillAllTweens();
        
        // Revert animations that haven't started yet
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteStartTime > time).Reverse())
        {
            animation.Animation.Revert();
        }
        
        // Apply all animations that should be complete by this time
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteEndTime <= time))
        {
            animation.Animation.ApplyEndState();
        }
        
        // Evaluate animations that we're in the middle of
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteStartTime <= time && a.AbsoluteEndTime > time))
        {
            var elapsedTime = time - animation.AbsoluteStartTime;
            animation.Animation.EvaluateAtTime(elapsedTime);
        }
        
        _currentTime = time;
        _timeAccumulator = 0;
        _startFromTime = time;

        if (_isPlaying)
        {
            Play();
        }
    }
    // Add pause/resume methods
    public void Pause()
    {
        if (!_isPlaying) return;

        _isPlaying = false;
        
        // Pause all tweens
        foreach (var tween in GetTree().GetProcessedTweens())
        {
            if (tween.IsValid())
            {
                tween.Pause();
            }
        }
    }

    public void Resume()
    {
        if (_isPlaying) return;

        _isPlaying = true;
        
        // Resume all tweens
        var hasValidTweens = false;
        foreach (var tween in GetTree().GetProcessedTweens())
        {
            if (tween.IsValid())
            {
                tween.Play();
                hasValidTweens = true;
            }
        }
        
        if (!hasValidTweens)
        {
            Play();
        }
    }
    
    private void Play()
    {
        _isPlaying = true;
        _timeAccumulator = 0;
        Engine.TimeScale = _playbackSpeed;
        
        // Kill all existing tweens
        KillAllTweens();
        
        var remainingDuration = TotalDuration - _currentTime;
        if (remainingDuration <= 0) return;
        
        // Filter animations that haven't finished yet
        var futureAnimations = _flattenedAnimations
            .Where(a => a.AbsoluteEndTime > _currentTime)
            .ToList();
        
        if (futureAnimations.Count == 0) return;
        
        // Group animations into non-overlapping tracks
        var tracks = BuildTracks(futureAnimations);
        
        GD.Print($"Created {tracks.Count} tracks for {futureAnimations.Count} animations");
        
        // Create a tween for each track
        for (int i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            var trackTween = i == 0 ? CreateTween() : GetTree().CreateTween();
            
            if (i == 0)
            {
                _masterTween = trackTween;
            }
            
            var trackCurrentTime = _currentTime;
            
            foreach (var animation in track.OrderBy(a => a.AbsoluteStartTime))
            {
                // Add delay to reach this animation's start time
                if (animation.AbsoluteStartTime > trackCurrentTime)
                {
                    var delay = animation.AbsoluteStartTime - trackCurrentTime;
                    trackTween.TweenInterval(delay);
                    trackCurrentTime = animation.AbsoluteStartTime;
                }
                
                // Calculate elapsed time for this specific animation
                var changeElapsedTime = Math.Max(0, _currentTime - animation.AbsoluteStartTime);
                animation.Animation.AppendTweener(trackTween, changeElapsedTime);
                
                // GD.Print($"  Added {animation.Animation.Name} to track {i} at {animation.AbsoluteStartTime}s");
                
                trackCurrentTime = animation.AbsoluteEndTime;
            }
        }
        
        // Add a final callback to the main tween to mark completion
        _masterTween.TweenCallback(Callable.From(() => 
        {
            _currentTime = TotalDuration;
            _isPlaying = false;
            GD.Print("Sequence completed");
        }));
        GD.Print("Finished starting of the playing");
    }
    
    private List<List<FlattenedAnimation>> BuildTracks(List<FlattenedAnimation> animations)
    {
        var tracks = new List<List<FlattenedAnimation>>();
        
        foreach (var animation in animations)
        {
            // Find a track where this animation doesn't overlap with existing animations
            var track = tracks.FirstOrDefault(t => 
                !t.Any(existing => 
                    animation.AbsoluteStartTime < existing.AbsoluteEndTime && 
                    animation.AbsoluteEndTime > existing.AbsoluteStartTime));
            
            if (track == null)
            {
                // Create a new track if no non-overlapping track exists
                track = new List<FlattenedAnimation>();
                tracks.Add(track);
            }
            
            track.Add(animation);
        }
        
        return tracks;
    }
    
    private void KillAllTweens()
    {
        foreach (var tween in GetTree().GetProcessedTweens())
        {
            tween.Kill();
        }
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
