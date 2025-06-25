using System.Linq;
using Godot;

[Tool]
public partial class TweenSequenceController : Control
{
    private TweenSequence _tweenSequence;
    private Button _playPauseButton;
    private Label _timeDisplay;
    private HSlider _timelineSlider;
    private bool _isDragging = false;
    private Button _resetButton;
    private SpinBox _playbackSpeedSpinBox;
    private SpinBox _seekTimeSpinBox;
    private Button _seekButton;

    public override void _Ready()
    {
        // Find the TweenSequence
        _tweenSequence = GetParent().GetChildren()
            .OfType<TweenSequence>()
            .FirstOrDefault();

        if (_tweenSequence == null)
        {
            GD.PrintErr("No tween sequence found");
            return;
        }
        
        _playPauseButton = GetNode<Button>("%Play");
        _playPauseButton.Pressed += OnPlayPausePressed;

        _resetButton = GetNode<Button>("%Reset");
        _resetButton.Pressed += OnResetPressed;

        _playbackSpeedSpinBox = GetNode<SpinBox>("%SpeedSpinBox");
        _playbackSpeedSpinBox.SetValueNoSignal(1.0);  // Explicitly set without triggering signals
        
        _seekTimeSpinBox = GetNode<SpinBox>("%SeekTimeSpinBox");
        _seekTimeSpinBox.MaxValue = _tweenSequence.TotalDuration;
        
        _seekButton = GetNode<Button>("%SeekButton");
        _seekButton.Pressed += OnSeekButtonPressed;

        _timeDisplay = GetNode<Label>("%TimeDisplay");
        
        _timelineSlider = GetNode<HSlider>("%TimelineSlider");
        _timelineSlider.DragStarted += OnSliderDragStarted;
        _timelineSlider.DragEnded += OnSliderDragEnded;
        _timelineSlider.ValueChanged += OnSliderValueChanged;
        _timelineSlider.MaxValue = _tweenSequence.TotalDuration;
    }

    public override void _Process(double delta)
    {
        if (_tweenSequence == null) return;

        // Update time display
        _timeDisplay.Text = $"{_tweenSequence.CurrentTime:F2} / {_tweenSequence.TotalDuration:F2}";

        // Update button state
        _playPauseButton.Text = _tweenSequence.IsPlaying ? "Pause" : "Play";

        // Update slider position if not dragging
        if (!_isDragging)
        {
            _timelineSlider.SetValueNoSignal(_tweenSequence.CurrentTime);
        }

        // Update playback speed if supported (you'd need to add this to TweenSequence)
        _tweenSequence.PlaybackSpeed = _playbackSpeedSpinBox.Value;
    }

    private void OnPlayPausePressed()
    {
        if (_tweenSequence == null) return;

        if (_tweenSequence.IsPlaying)
            _tweenSequence.Pause();
        else
            _tweenSequence.Resume();
    }

    private void OnResetPressed()
    {
        if (_tweenSequence == null) return;

        _tweenSequence.SeekTo(0);
    }

    private void OnSliderDragStarted()
    {
        _isDragging = true;

        // Pause during scrubbing for smoother experience
        if (_tweenSequence != null && _tweenSequence.IsPlaying)
        {
            _tweenSequence.Pause();
        }
    }

    private void OnSliderDragEnded(bool valueChanged)
    {
        _isDragging = false;
    }

    private void OnSliderValueChanged(double value)
    {
        if (_tweenSequence == null || !_isDragging) return;
        
        _tweenSequence.SeekTo(value);
    }
    
    private void OnSeekButtonPressed()
    {
        if (_tweenSequence == null) return;
        
        _tweenSequence.SeekTo(_seekTimeSpinBox.Value);
    }
}
