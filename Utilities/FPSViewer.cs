using Godot;

namespace PrimerTools;

[Tool]
public partial class FPSViewer : Label
{
    [Export] private int _maxFPS;
    [Export] private bool _printing;

    /// <summary>
    /// When true, the label also shows per-frame draw calls, primitives, and
    /// _Process CPU time. Cheap to compute; useful when diagnosing whether
    /// the bottleneck is on the GPU (primitive/draw-call counts move) or the
    /// CPU (process time moves) as you toggle features on and off.
    /// </summary>
    [Export] private bool _showRenderStats = true;

    private double _printInterval = 1;
    private double _timer;

    public override void _Ready()
    {
        Engine.MaxFps = _maxFPS;
    }

    public override void _Process(double delta)
    {
        int fps = (int)Engine.GetFramesPerSecond();
        string text = $"{fps} FPS";

        if (_showRenderStats)
        {
            long drawCalls = (long)Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
            long prims = (long)Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
            double procMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
            text += $"\nDraws: {drawCalls}\nPrims: {prims:N0}\nProc: {procMs:F2} ms";
        }

        Text = text;

        if (!_printing) return;
        _timer += delta;
        if (!(_timer >= _printInterval)) return;
        GD.Print(text);
        _timer -= _printInterval;
    }
}