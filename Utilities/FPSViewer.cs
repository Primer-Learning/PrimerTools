using Godot;

namespace PrimerTools;

public partial class FPSViewer : Label
{
    public override void _Ready()
    {
        Engine.MaxFps = 0;
    }

    public override void _Process(double delta)
    {
        Text = $"{Engine.GetFramesPerSecond()} FPS";
    }
}