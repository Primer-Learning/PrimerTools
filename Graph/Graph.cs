using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PrimerTools.Graph;

[Tool]
public partial class Graph : Node3D
{
    // private MemberChangeChecker memberChangeChecker;
    public override void _Process(double delta)
    {
        // memberChangeChecker ??= new MemberChangeChecker(this);
        // if (Engine.IsEditorHint() && memberChangeChecker.CheckForChanges())
        // {
        //     Update();
        // }
    }
    
    public static Graph CreateAsOwnedChild(Node parent)
    {
        if (parent.HasNode("Graph"))
        {
            // I'm not sure this ever happens.
            GD.PushWarning("Graph already exists, which is against the intended use pattern.");
            return parent.GetNode<Graph>("Graph");   
        }
        
        // Make the graph
        var graph = new Graph();
        graph.Name = "Graph";
        parent.AddChild(graph);
        graph.InstantiateAxes();

        // Make the graph and children visible in the editor and eligible to be saved in the scene. 
        var sceneRoot = graph.GetTree().EditedSceneRoot;
        graph.Owner = sceneRoot;
        graph.MakeSelfAndChildrenLocal(sceneRoot);
        
        return graph;
    }

    // public override void _Ready()
    // {
    //     if (Engine.IsEditorHint()) InstantiateAxes();
    // }
    
    private void InstantiateAxes()
    {
        var axisScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/PackedScenes/axis.tscn"); 
        var x = axisScene.Instantiate<Axis>();
        var y = axisScene.Instantiate<Axis>();
        var z = axisScene.Instantiate<Axis>();
        y.ticScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/PackedScenes/axis_tic_y.tscn");
        y.RotationDegrees = new Vector3(0, 0, 90);
        z.ticScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/PackedScenes/axis_tic_z.tscn");
        z.RotationDegrees = new Vector3(0, -90, 0);
        
        AddChild(x);
        AddChild(y);
        AddChild(z);
        x.Name = "X";
        y.Name = "Y";
        z.Name = "Z";
    }
    
    private bool _transitionTicsAllTogether = false;
    public bool transitionTicsAllTogether
    {
        get => _transitionTicsAllTogether;
        set
        {
            _transitionTicsAllTogether = value;
            foreach (var axis in Axes)
                axis.transitionTicsAllTogether = value;
        }
    }

    public Axis XAxis => GetNode<Axis>("X"); 
    public Axis YAxis => GetNode<Axis>("Y"); 
    public Axis ZAxis => GetNode<Axis>("Z");
    private List<Axis> Axes => new() { XAxis, YAxis, ZAxis };

    public Animation Transition(float duration = 0.5f)
    {
        var removeTransitions = new List<Animation>();
        var updateTransitions = new List<Animation>();
        var addTransitions = new List<Animation>();

        foreach (var axis in Axes)
        {
            var (remove, update, add) = axis.UpdateChildren();
            removeTransitions.Add(remove);
            updateTransitions.Add(update);
            addTransitions.Add(add);
        }
        // For data objects
        updateTransitions.AddRange(
            GetChildren().OfType<IPrimerGraphData>().Select(x => x.Transition(duration))
        );
        return AnimationUtilities.Series(
            removeTransitions.RunInParallel(),
            updateTransitions.RunInParallel(),
            addTransitions.RunInParallel()
        );
    }

    // Saving for reference when it's animation time
    // public Tween Transition()
    // {
    //     var removeTransitions = new List<Tween>();
    //     var updateTransitions = new List<Tween>();
    //     var addTransitions = new List<Tween>();
    //
    //     foreach (var axis in Axes)
    //     {
    //         if (axis.length == 0) continue;
    //         var (remove, update, add) = axis.PrepareTransitionParts();
    //         removeTransitions.Add(remove);
    //         updateTransitions.Add(update);
    //         addTransitions.Add(add);
    //     }
    //     updateTransitions.AddRange(
    //         GetComponentsInChildren<IPrimerGraphData>().Select(x => x.Transition())
    //     );
    //     return Tween.Series(
    //         removeTransitions.RunInParallel(),
    //         updateTransitions.RunInParallel(),
    //         addTransitions.RunInParallel()
    //     );
    // }

    // public Tween ShrinkPlottedDataToEnd()
    // {
    //     return GetComponentsInChildren<IPrimerGraphData>().Select(x => x.ShrinkToEnd()).RunInParallel();
    // }
    //
    // // We assume there will be no data present when the axes are drown, so they appear independently
    // public Tween Appear() => Axes.Select(x => x.Appear()).RunInParallel();
    // public Tween Disappear() => Axes.Select(x => x.Disappear()).RunInParallel();
    
    public CurvePlot2D AddLine()
    {
        var line = new CurvePlot2D();
        line.TransformPointFromDataSpaceToPositionSpace = DataSpaceToPositionSpace;
        AddChild(line);
        line.Owner = GetTree().EditedSceneRoot;
        return line;
    }
    
    // public StackedArea AddStackedArea(string name)
    // {
    //     var gnome = new SimpleGnome(transform);
    //     var area = gnome.Add<StackedArea>(name);
    //     area.Reset(); // In case it already existed
    //     area.transformPointFromDataSpaceToPositionSpace = DataSpaceToPositionSpace;
    //     return area;
    // }
    
    public BarPlot AddBarPlot(string name = "Bar Plot")
    {
        var barPlot = new BarPlot();
        barPlot.TransformPointFromDataSpaceToPositionSpace = DataSpaceToPositionSpace;
        AddChild(barPlot);
        barPlot.Owner = GetTree().EditedSceneRoot;
        barPlot.Name = name;
        return barPlot;
    }
    
    public Vector3 DataSpaceToPositionSpace(Vector3 point)
    {
        return new Vector3(
            (point.X - XAxis.min) / XAxis.RangeSize * XAxis.LengthMinusPadding,
            (point.Y - YAxis.min) / YAxis.RangeSize * YAxis.LengthMinusPadding,
            (point.Z - ZAxis.min) / ZAxis.RangeSize * ZAxis.LengthMinusPadding
        );
    }
    
    
}
