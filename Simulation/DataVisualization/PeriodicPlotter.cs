using Godot;
using PrimerTools.Graph;

public partial class PeriodicPlotter : Node
{
    public CurvePlot2D Curve;

    [Export] public bool Plotting = true;
    private double _timeSinceLastPlot;
    public override void _Process(double delta)
    {
        if (!Plotting) return;
        _timeSinceLastPlot += delta;
        if (_timeSinceLastPlot < 1) return;
        PlotData();
        _timeSinceLastPlot -= 1;
    }
    
    private void PlotData()
    {
        var numPoints = Curve.GetData().Length;
        Curve.FetchData();
        if (numPoints == Curve.GetData().Length) return;
        Curve.TweenTransition(1);
    }
}