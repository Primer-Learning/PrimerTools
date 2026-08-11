using Godot;
using System.Collections.Generic;

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

    /// <summary>
    /// Frames slower than this count toward the per-second hitch counter.
    /// With a 10 Hz simulation, the hitch count approximates how many sim
    /// ticks were heavy in the last second, while the worst-frame readout
    /// shows how heavy the worst one was.
    /// </summary>
    [Export] private double _hitchThresholdMs = 25;

    private double _printInterval = 1;
    private double _timer;

    // Rolling one-second window of real frame times for the worst-frame and
    // hitch-count readouts. Measured by wall clock between _Process calls
    // rather than the delta argument, so the numbers stay honest if
    // Engine.TimeScale is ever changed.
    private readonly Queue<(ulong endUsec, double ms)> _frameWindow = new();
    private ulong _lastFrameUsec;

    public override void _Ready()
    {
        Engine.MaxFps = _maxFPS;
    }

    public override void _Process(double delta)
    {
        ulong now = Time.GetTicksUsec();
        if (_lastFrameUsec != 0)
        {
            _frameWindow.Enqueue((now, (now - _lastFrameUsec) / 1000.0));
        }
        _lastFrameUsec = now;

        while (_frameWindow.Count > 0 && now - _frameWindow.Peek().endUsec > 1_000_000)
        {
            _frameWindow.Dequeue();
        }

        double worstMs = 0;
        int hitchCount = 0;
        foreach (var (_, ms) in _frameWindow)
        {
            if (ms > worstMs) worstMs = ms;
            if (ms > _hitchThresholdMs) hitchCount++;
        }

        int fps = (int)Engine.GetFramesPerSecond();
        string text = $"{fps} FPS";
        text += $"\nWorst: {worstMs:F1} ms";
        text += $"\nOver {_hitchThresholdMs:0} ms: {hitchCount}";

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
