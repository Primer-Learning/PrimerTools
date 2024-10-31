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
        graph.ZAxis.length = 0;
        
        graph.XAxis.Max = 5;
        graph.YAxis.Max = 10;
        graph.ZAxis.Max = 5;

        graph.XAxis.TicStep = 1;
        graph.YAxis.TicStep = 2;
        graph.ZAxis.TicStep = 1;
        graph.Transition();
        
        // Create the stacked bar plot
        var stackedBarPlot = graph.AddStackedBarPlot();
        
        // Create sample data sets
        var data1 = new List<List<float>>
        {
            new List<float> { 1.0f, 2.0f, 3.0f }, // First stack (bottom to top)
            new List<float> { 2.0f, 1.0f, 2.0f }, // Second stack
            new List<float> { 1.0f, 2.0f, 1.0f }, // Third stack
            new List<float> { 3.0f, 1.0f, 2.0f }, // Fourth stack
            new List<float> { 2.0f, 2.0f, 1.0f }  // Fifth stack
        };
        
        var data2 = new List<List<float>>
        {
            new List<float> { 2.0f, 1.0f, 1.0f }, // First stack (bottom to top)
            new List<float> { 1.0f, 3.0f, 1.0f }, // Second stack
            new List<float> { 2.0f, 2.0f, 2.0f }, // Third stack
            new List<float> { 1.0f, 2.0f, 3.0f }, // Fourth stack
            new List<float> { 2.0f, 1.0f, 2.0f }  // Fifth stack
        };

        var datas = new List<List<List<float>>> { data1, data2 };
        
        stackedBarPlot.ShowValuesOnBars = true;
        stackedBarPlot.BarLabelScaleFactor = 0.6f;
        
        var index = 0;
        while (_run)
        {
            stackedBarPlot.Data = datas[index++ % datas.Count];
            var tween = stackedBarPlot.TweenTransition();
            await tween.ToSignal(tween, Tween.SignalName.Finished);
        }
    }
}
