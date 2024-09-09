using Godot;
using PrimerTools.Graph;

public partial class PeriodicPlotter : Node
{
    public CurvePlot2D Curve;

    [Export] public bool Plotting;
    [Export] private float _plottingInterval = 1;
    private double _timeSinceLastPlot;
    public override void _Process(double delta)
    {
        if (!Plotting) return;
        _timeSinceLastPlot += delta;
        if (_timeSinceLastPlot < _plottingInterval) return;
        PlotData();
        _timeSinceLastPlot -= _plottingInterval;
    }
    
    private void PlotData()
    {
        var numPoints = Curve.GetData().Length;
        Curve.FetchData();
        if (numPoints == Curve.GetData().Length) return;
        Curve.TweenTransition(1);
    }
}