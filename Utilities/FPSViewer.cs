using Godot;

namespace PrimerTools;

[Tool]
public partial class FPSViewer : Label
{
    [Export] private bool _printing;
    private double _printInterval = 1;
    private double _timer;
    
    public override void _Ready()
    {
        Engine.MaxFps = 0;
    }

    public override void _Process(double delta)
    {
        var text = $"{Engine.GetFramesPerSecond()} FPS";
        Text = text;
        
        if (!_printing) return;
        _timer += delta;
        if (!(_timer >= _printInterval)) return;
        GD.Print(text);
        _timer -= _printInterval;
    }
}