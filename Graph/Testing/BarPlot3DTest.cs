using System.Collections.Generic;
using Godot;
using PrimerTools.Graph;

[Tool]
public partial class BarPlot3DTest : Node3D
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
        
        graph.XAxis.Max = 3;
        graph.YAxis.Max = 5;
        graph.ZAxis.Max = 3;

        graph.XAxis.TicStep = 1;
        graph.YAxis.TicStep = 1;
        graph.ZAxis.TicStep = 1;
        graph.Transition();
        
        // Create the bar plot
        graph.Owner = GetTree().EditedSceneRoot;
        var barPlot = graph.AddBarPlot3D();
        // var barPlot = new BarPlot3D(3, 3); // 3x3 grid
        // graph.AddChild(barPlot);
        
        // Create sample data (9 values for 3x3 grid)
        var data = new float[,]
        {
            { 1.0f, 2.0f, 3.0f },
            { 2.0f, 4.0f, 2.0f },
            { 3.0f, 1.0f, 2.0f }
        };
        var data2 = new float[,]
        {
            { 2.0f, 1.0f, 4.0f },
            { 1.0f, 5.0f, 1.0f },
            { 4.0f, 0.0f, 3.0f }
        };
        var datas = new List<float[,]>();
        datas.Add(data);
        datas.Add(data2);

        var index = 0;
        GD.Print(_run);
        while (_run)
        {
            barPlot.DataFetchMethod = () => datas[index++ % datas.Count];
            barPlot.FetchData();
            var tween = barPlot.TweenTransition();
            await tween.ToSignal(tween, Tween.SignalName.Finished);
        }
    }
}
