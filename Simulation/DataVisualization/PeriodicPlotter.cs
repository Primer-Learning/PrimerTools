using System.Linq;
using Godot;
using PrimerTools.Graph;

public partial class PeriodicPlotter : Node
{
    public Graph graph;

    [Export] public bool Plotting;
    [Export] public double PlottingInterval = 1;
    private double _timeSinceLastPlot;
    public override void _Process(double delta)
    {
        if (!Plotting) return;
        _timeSinceLastPlot += delta;
        if (_timeSinceLastPlot < PlottingInterval) return;
        PlotData();
        _timeSinceLastPlot -= PlottingInterval;
    }
    
    private void PlotData(double duration = -1)
    {
        if (duration < 0) duration = PlottingInterval;
        foreach (var dataPlotter in graph.GetChildren().OfType<IPrimerGraphData>())
        {
            dataPlotter.FetchData();
            dataPlotter.TweenTransition(duration);
        }
    }
    
    public void PlotNow(double duration = -1)
    {
        PlotData(duration);
        _timeSinceLastPlot = 0;
    }
}