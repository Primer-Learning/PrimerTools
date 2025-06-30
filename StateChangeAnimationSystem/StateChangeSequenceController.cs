using System.Linq;
using Godot;

public partial class StateChangeSequenceController : Control
{
    private StateChangeSequence _stateChangeSequence;
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
        _stateChangeSequence = GetParent().GetChildren()
            .OfType<StateChangeSequence>()
            .FirstOrDefault();

        if (_stateChangeSequence == null)
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
        _seekTimeSpinBox.MaxValue = _stateChangeSequence.TotalDuration;
        _seekTimeSpinBox.Value = _stateChangeSequence.StartFromTime;
        
        _seekButton = GetNode<Button>("%SeekButton");
        _seekButton.Pressed += OnSeekButtonPressed;

        _timeDisplay = GetNode<Label>("%TimeDisplay");
        
        _timelineSlider = GetNode<HSlider>("%TimelineSlider");
        _timelineSlider.DragStarted += OnSliderDragStarted;
        _timelineSlider.DragEnded += OnSliderDragEnded;
        _timelineSlider.ValueChanged += OnSliderValueChanged;
        _timelineSlider.MaxValue = _stateChangeSequence.TotalDuration;
    }

    public override void _Process(double delta)
    {
        if (_stateChangeSequence == null) return;

        // Update time display
        _timeDisplay.Text = $"{_stateChangeSequence.CurrentTime:F2} / {_stateChangeSequence.TotalDuration:F2}";

        // Update button state
        _playPauseButton.Text = _stateChangeSequence.IsPlaying ? "Pause" : "Play";

        // Update slider position if not dragging
        if (!_isDragging)
        {
            _timelineSlider.SetValueNoSignal(_stateChangeSequence.CurrentTime);
        }

        // Update playback speed if supported (you'd need to add this to TweenSequence)
        _stateChangeSequence.PlaybackSpeed = _playbackSpeedSpinBox.Value;
    }

    private void OnPlayPausePressed()
    {
        if (_stateChangeSequence == null) return;

        if (_stateChangeSequence.IsPlaying)
            _stateChangeSequence.Pause();
        else
            _stateChangeSequence.Resume();
    }

    private void OnResetPressed()
    {
        if (_stateChangeSequence == null) return;

        _stateChangeSequence.SeekTo(0);
    }

    private void OnSliderDragStarted()
    {
        _isDragging = true;

        // Pause during scrubbing for smoother experience
        if (_stateChangeSequence != null && _stateChangeSequence.IsPlaying)
        {
            _stateChangeSequence.Pause();
        }
    }

    private void OnSliderDragEnded(bool valueChanged)
    {
        _isDragging = false;
    }

    private void OnSliderValueChanged(double value)
    {
        if (_stateChangeSequence == null || !_isDragging) return;
        
        _stateChangeSequence.SeekTo(value);
    }
    
    private void OnSeekButtonPressed()
    {
        if (_stateChangeSequence == null) return;
        
        _stateChangeSequence.SeekTo(_seekTimeSpinBox.Value);
    }
}
