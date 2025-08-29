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
    
    [Export] private int _startFromMinutes = 0;
    [Export] private double _startFromSeconds = 0;
    public double StartTimeInSeconds => _startFromMinutes * 60 + _startFromSeconds;
    
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

        if (StartTimeInSeconds < 0) _startFromSeconds = TotalDuration;
        SeekTo(StartTimeInSeconds);
        
        if (StartTimeInSeconds < TotalDuration && _playbackSpeed > 0)
        {
            Play();
        }
    }

    private void GatherSubsequences()
    {
        foreach (var child in GetChildren())
        {
            if (child is StateChangeSequence { Active: true } subsequence)
            {
                subsequence.Define();
                
                if (_combineSequencesInParallel)
                {
                    // All sequences start at time 0
                    _rootComposite.AddStateChange(subsequence.RootComposite, 0);
                }
                else
                {
                    // Current behavior: sequences in series
                    _rootComposite.AddStateChangeWithDelay(subsequence.RootComposite);
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
            var recorder = GetParent().GetChildren().OfType<SceneRecorder>().FirstOrDefault();
            recorder?.OnSequenceComplete();
        }
        
        SeekTo(newTime);
    }
    
    public void SeekTo(double time)
    {
        if (time < 0) time = TotalDuration;
        
        // Sync audio position if we have an audio player
        if (_audioPlayer != null)
        {
            if (_isPlaying && !_audioPlayer.Playing)
            {
                _audioPlayer.Play((float)time);
            }
            else if (_audioPlayer.Playing)
            {
                // Only seek if we're significantly out of sync
                var audioPosition = _audioPlayer.GetPlaybackPosition();
                if (Mathf.Abs(audioPosition - time) > 0.05)
                {
                    _audioPlayer.Seek((float)time);
                }
            }
        }
        
        // We're going back in time! Revert animations that shouldn't have started yet.
        if (time < _currentTime)
        {
            foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteStartTime > time).Reverse())
            {
                animation.Animation.Revert();
            }
        }
        
        // Apply animations completed during the time change we're applying
        foreach (var animation in _flattenedAnimations.Where(a => a.AbsoluteEndTime <= time && a.AbsoluteEndTime > _currentTime))
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

        _currentTime = time;
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
        
        // Start audio playback
        if (_audioPlayer != null)
        {
            _audioPlayer.Play((float)_currentTime);
        }
    }
}
