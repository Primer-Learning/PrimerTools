using Godot;
using PrimerTools.Graph;

public partial class PeriodicPlotter : Node
{
    public CurvePlot2D Curve;
    public BarPlot BarPlot;
    public BarPlot3D BarPlot3D;

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
        if (Curve != null)
        {
            // TODO: Delete the numPoints lines and test. This method could probably just iterate through IPrimerData
            var numPoints = Curve.GetData().Length;
            Curve.FetchData();
            if (numPoints == Curve.GetData().Length) return;
            Curve.TweenTransition(1);
        }

        if (BarPlot != null)
        {
            BarPlot.FetchData();
            BarPlot.TweenTransition(1);
        }

        if (BarPlot3D != null)
        {
            BarPlot3D.FetchData();
            BarPlot3D.TweenTransition(1);
        }
    }
}