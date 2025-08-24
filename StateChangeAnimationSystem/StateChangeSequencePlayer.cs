using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools;

public partial class StateChangeSequencePlayer : Node
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
    
    [Export] private bool _combineSequencesInParallel = false;
    
    [Export] private AudioStream _audioTrack;
    private AudioStreamPlayer _audioPlayer;
    
    [Export(PropertyHint.Range, "-80,24,0.1")] 
    private float _volumeDb = 0.0f;
    public float VolumeDb
    {
        get => _volumeDb;
        set
        {
            _volumeDb = Mathf.Clamp(value, -80.0f, 24.0f);
            if (_audioPlayer != null)
            {
                _audioPlayer.VolumeDb = _volumeDb;
            }
        }
    }
    
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
        set
        {
            _playbackSpeed = Mathf.Max(0.0, value);
            if (_audioPlayer != null)
            {
                _audioPlayer.PitchScale = (float)_playbackSpeed;
            }
        }
    }
    
    public override void _Ready()
    {
        // Set up audio player if audio track is provided
        if (_audioTrack != null)
        {
            _audioPlayer = new AudioStreamPlayer();
            AddChild(_audioPlayer);
            _audioPlayer.Stream = _audioTrack;
            _audioPlayer.PitchScale = (float)_playbackSpeed;
            _audioPlayer.VolumeDb = _volumeDb;
        }
        
        GatherSubsequences();

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

    private void GatherSubsequences()
    {
        foreach (var child in GetChildren())
        {
            if (child is StateChangeSequence subsequence)
            {
                subsequence.Define();
                
                if (_combineSequencesInParallel)
                {
                    // All sequences start at time 0
                    _rootComposite.AddStateChangeAt(subsequence.RootComposite, 0);
                }
                else
                {
                    // Current behavior: sequences in series
                    _rootComposite.AddStateChange(subsequence.RootComposite);
                }
            }
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
            
            // Stop audio when sequence completes
            _audioPlayer?.Stop();
            
            // Notify recorder if present
            var recorder = GetParent()?.GetChildren().OfType<SceneRecorder>().FirstOrDefault();
            recorder?.OnSequenceComplete();
        }
        
        // Use the existing seek logic for frame-by-frame updates
        SeekToInternal(newTime);
        _currentTime = newTime;
    }
    
    private void SeekToInternal(double time)
    {
        if (time < 0)
        {
            GD.Print(TotalDuration);
            time = TotalDuration;
        }
        
        // Sync audio position if we have an audio player
        if (_audioPlayer != null)
        {
            if (_isPlaying && !_audioPlayer.Playing)
            {
                _audioPlayer.Play((float)time);
            }
            else if (_audioPlayer.Playing)
            {
                // Only seek if we're significantly out of sync (more than 50ms)
                var audioPosition = _audioPlayer.GetPlaybackPosition();
                if (Mathf.Abs(audioPosition - time) > 0.05)
                {
                    _audioPlayer.Seek((float)time);
                }
            }
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
        
        // Seek audio to match
        if (_audioPlayer != null)
        {
            _audioPlayer.Stop();
            if (_isPlaying)
            {
                _audioPlayer.Play((float)time);
            }
        }
    }
    
    // Add pause/resume methods
    public void Pause()
    {
        _isPlaying = false;
        if (_audioPlayer != null)
        {
            _audioPlayer.StreamPaused = true;
        }
    }

    public void Resume()
    {
        _isPlaying = true;
        if (_audioPlayer != null)
        {
            _audioPlayer.StreamPaused = false;
            if (!_audioPlayer.Playing)
            {
                _audioPlayer.Play((float)_currentTime);
            }
        }
    }
    
    private void Play()
    {
        _isPlaying = true;
        _timeAccumulator = 0;
        
        // Start audio playback
        if (_audioPlayer != null)
        {
            _audioPlayer.Play((float)_currentTime);
        }
    }
}
