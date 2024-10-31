using System.Collections.Generic;
using Godot;
using PrimerTools.Graph;

[Tool]
public partial class StackedBarPlotTest : Node3D
{
    private bool _run;
    [Export]
    public bool Run
    {
        get => _run;
        set
        {
            _run = value;
            if (value)
            {
                // Clear any existing children
                foreach (var child in GetChildren())
                {
                    child.Free();
                }
                
                CreateGraph();
            }
        }
    }

    private async void CreateGraph()
    {
        // Create the graph
        var graph = Graph.CreateInstance();
        AddChild(graph);
        
        // Configure the axes
        graph.XAxis.length = 10;
        graph.YAxis.length = 10;
        graph.ZAxis.length = 10;
        
        graph.XAxis.Max = 5;
        graph.YAxis.Max = 10;
        graph.ZAxis.Max = 5;

        graph.XAxis.TicStep = 1;
        graph.YAxis.TicStep = 2;
        graph.ZAxis.TicStep = 1;
        graph.Transition();
        
        // Create the stacked bar plot
        graph.Owner = GetTree().EditedSceneRoot;
        var stackedBarPlot = new StackedBarPlot();
        graph.AddChild(stackedBarPlot);
        
        // Create sample data sets
        var data1 = new List<List<float>>
        {
            new() { 1.0f, 2.0f, 3.0f, 2.0f, 1.0f }, // First stack segment
            new() { 2.0f, 1.0f, 2.0f, 3.0f, 2.0f }, // Second stack segment
            new() { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f }  // Third stack segment
        };
        
        var data2 = new List<List<float>>
        {
            new() { 2.0f, 1.0f, 2.0f, 1.0f, 2.0f },
            new() { 1.0f, 2.0f, 3.0f, 2.0f, 1.0f },
            new() { 2.0f, 1.0f, 1.0f, 1.0f, 2.0f }
        };

        var datas = new List<List<List<float>>> { data1, data2 };
        
        stackedBarPlot.ShowValuesOnBars = true;
        stackedBarPlot.BarLabelScaleFactor = 0.5f;
        
        var index = 0;
        while (_run)
        {
            stackedBarPlot.Data = datas[index++ % datas.Count];
            var tween = stackedBarPlot.TweenTransition();
            await tween.ToSignal(tween, Tween.SignalName.Finished);
        }
    }
}
