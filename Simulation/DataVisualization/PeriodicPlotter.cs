using System.Linq;
using Godot;
using PrimerTools.Graph;

public partial class PeriodicPlotter : Node
{
    public Graph graph;

    [Export] public bool Plotting;
    [Export] private double _plottingInterval = 1;
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
        foreach (var dataPlotter in graph.GetChildren().OfType<IPrimerGraphData>())
        {
            dataPlotter.FetchData();
            dataPlotter.TweenTransition(_plottingInterval);
        }
    }
}