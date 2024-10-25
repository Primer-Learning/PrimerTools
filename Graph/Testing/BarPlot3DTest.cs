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
            if (value)
            {
                // Clear any existing children
                foreach (var child in GetChildren())
                {
                    child.Free();
                }
                
                CreateGraph();
            }
            _run = value;
        }
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        Run = true;
    }

    private void CreateGraph()
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
        
        // Create the bar plot
        var barPlot = new BarPlot3D(3, 3); // 3x3 grid
        graph.AddChild(barPlot);
        graph.Owner = GetTree().EditedSceneRoot;
        barPlot.Owner = GetTree().EditedSceneRoot;
        
        // Create sample data (9 values for 3x3 grid)
        var data = new float[3, 3]
        {
            { 1.0f, 2.0f, 3.0f },
            { 2.0f, 4.0f, 2.0f },
            { 3.0f, 1.0f, 2.0f }
        };
        
        // // Assign data and transition
        barPlot.DataFetchMethod = () => data;
        barPlot.FetchData();
        barPlot.Transition();
        
        // Initial graph setup
        // graph.Transition();
    }
}
