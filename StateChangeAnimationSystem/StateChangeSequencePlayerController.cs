using System.Linq;
using Godot;
using PrimerTools;

public partial class StateChangeSequencePlayerController : Control
{
    private StateChangeSequencePlayer _stateChangeSequencePlayer;
    private Button _playPauseButton;
    private Label _timeDisplay;
    private HSlider _timelineSlider;
    private bool _isDragging = false;
    private Button _resetButton;
    private SpinBox _playbackSpeedSpinBox;
    private SpinBox _seekTimeSpinBox;
    private Button _seekButton;
    
    private Button _hideButton;
    private bool _hidden = false;

    public override void _Ready()
    {
        _stateChangeSequencePlayer = GetParent().GetChildren()
            .OfType<StateChangeSequencePlayer>()
            .FirstOrDefault();
        
        if (_stateChangeSequencePlayer == null)
        {
            GD.PrintErr("No tween sequence found");
            return;
        }
        
        if (_stateChangeSequencePlayer.TotalDuration == 0)
        {
            GD.PrintErr("State change sequence player has zero duration. It might not be Ready.");
            if (_stateChangeSequencePlayer.IsNodeReady())
            {
                GD.PrintErr("Apparently, IsNodeReady can be true before Ready has been called on the node.");
            }
        }
        
        _hideButton = GetNode<Button>("%HideButton");
        _hideButton.Pressed += OnHideButtonPressed;
        
        _playPauseButton = GetNode<Button>("%Play");
        _playPauseButton.Pressed += OnPlayPausePressed;

        _resetButton = GetNode<Button>("%Reset");
        _resetButton.Pressed += OnResetPressed;

        _playbackSpeedSpinBox = GetNode<SpinBox>("%SpeedSpinBox");
        _playbackSpeedSpinBox.SetValueNoSignal(1.0);  // Explicitly set without triggering signals
        
        _seekTimeSpinBox = GetNode<SpinBox>("%SeekTimeSpinBox");
        _seekTimeSpinBox.MaxValue = _stateChangeSequencePlayer.TotalDuration;
        _seekTimeSpinBox.Value = _stateChangeSequencePlayer.StartFromTime;
        
        _seekButton = GetNode<Button>("%SeekButton");
        _seekButton.Pressed += OnSeekButtonPressed;
        _seekButton = GetNode<Button>("%SetSeekPointButton");
        _seekButton.Pressed += SetSeekPoint;

        _timeDisplay = GetNode<Label>("%TimeDisplay");
        
        _timelineSlider = GetNode<HSlider>("%TimelineSlider");
        _timelineSlider.DragStarted += OnSliderDragStarted;
        _timelineSlider.DragEnded += OnSliderDragEnded;
        _timelineSlider.ValueChanged += OnSliderValueChanged;
        _timelineSlider.MaxValue = _stateChangeSequencePlayer.TotalDuration;
    }

    public override void _Process(double delta)
    {
        if (_stateChangeSequencePlayer == null) return;

        // Update time display
        _timeDisplay.Text = $"{_stateChangeSequencePlayer.CurrentTime:F2} / {_stateChangeSequencePlayer.TotalDuration:F2}";

        // Update button state
        _playPauseButton.Text = _stateChangeSequencePlayer.IsPlaying ? "Pause" : "Play";

        // Update slider position if not dragging
        if (!_isDragging)
        {
            _timelineSlider.SetValueNoSignal(_stateChangeSequencePlayer.CurrentTime);
        }

        // Update playback speed if supported (you'd need to add this to TweenSequence)
        _stateChangeSequencePlayer.PlaybackSpeed = _playbackSpeedSpinBox.Value;
    }

    private void OnHideButtonPressed()
    {
        if (!_hidden)
        {
            GetNode<Control>("%ControlsContainer").Visible = false;
            GetNode<Control>("%ScrubberContainer").Visible = false;
            _hideButton.Text = "Show";
        }
        else
        {
            GetNode<Control>("%ControlsContainer").Visible = true;
            GetNode<Control>("%ScrubberContainer").Visible = true;
            _hideButton.Text = "Hide Controls";
        }

        _hidden = !_hidden;
    }
    
    private void OnPlayPausePressed()
    {
        if (_stateChangeSequencePlayer == null) return;

        if (_stateChangeSequencePlayer.IsPlaying)
            _stateChangeSequencePlayer.Pause();
        else
            _stateChangeSequencePlayer.Resume();
    }

    private void OnResetPressed()
    {
        if (_stateChangeSequencePlayer == null) return;

        _stateChangeSequencePlayer.SeekTo(0);
    }

    private void OnSliderDragStarted()
    {
        _isDragging = true;

        // Pause during scrubbing for smoother experience
        if (_stateChangeSequencePlayer != null && _stateChangeSequencePlayer.IsPlaying)
        {
            _stateChangeSequencePlayer.Pause();
        }
    }

    private void OnSliderDragEnded(bool valueChanged)
    {
        _isDragging = false;
    }

    private void OnSliderValueChanged(double value)
    {
        if (_stateChangeSequencePlayer == null || !_isDragging) return;
        
        _stateChangeSequencePlayer.SeekTo(value);
    }
    
    private void OnSeekButtonPressed()
    {
        if (_stateChangeSequencePlayer == null) return;
        
        _stateChangeSequencePlayer.SeekTo(_seekTimeSpinBox.Value);
    }

    private void SetSeekPoint()
    {
        _seekTimeSpinBox.Value = _stateChangeSequencePlayer.CurrentTime;
    }
}
