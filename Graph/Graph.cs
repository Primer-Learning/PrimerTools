using Godot;
using System.Collections.Generic;
using System.Linq;
using PrimerTools.LaTeX;

namespace PrimerTools.Graph;

[Tool]
public partial class Graph : Node3D
{
    private Node3D _dataSpaceMin;
    private Node3D _dataSpaceMax;
    // private MemberChangeChecker memberChangeChecker;
    public override void _Process(double delta)
    {
        // memberChangeChecker ??= new MemberChangeChecker(this);
        // if (Engine.IsEditorHint() && memberChangeChecker.CheckForChanges())
        // {
        //     Update();
        // }
    }
    
    public static Graph CreateInstance()
    {
        // Make the graph
        var graph = new Graph();
        graph.Name = "Graph";
        graph.InstantiateAxes();
        
        // Initialize data space marker nodes
        graph._dataSpaceMin = new Node3D { Name = "DataSpaceMin" };
        graph._dataSpaceMax = new Node3D { Name = "DataSpaceMax" };
        graph.AddChild(graph._dataSpaceMin);
        graph.AddChild(graph._dataSpaceMax);
        
        return graph;
    }
    
    private void InstantiateAxes()
    {
        var axisScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis.tscn");
        var x = axisScene.Instantiate<Axis>();
        var y = axisScene.Instantiate<Axis>();
        var z = axisScene.Instantiate<Axis>();
        x.TicScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis_tic_x.tscn");
        y.TicScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis_tic_y.tscn");
        y.RotationDegrees = new Vector3(0, 0, 90);
        z.TicScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis_tic_z.tscn");
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
                axis.TransitionTicsAllTogether = value;
        }
    }

    public Axis XAxis => GetNode<Axis>("X"); 
    public Axis YAxis => GetNode<Axis>("Y"); 
    public Axis ZAxis => GetNode<Axis>("Z");
    private List<Axis> Axes => new() { XAxis, YAxis, ZAxis };

    public enum AxisLabelAlignmentOptions
    {
        Along,
        End
    }

    private AxisLabelAlignmentOptions _xAxisAlignment = AxisLabelAlignmentOptions.End;

    public AxisLabelAlignmentOptions XAxisAlignment
    {
        get => _xAxisAlignment;
        set
        {
            _xAxisAlignment = value;
            if (xAxisLabelLatexNode == null) return;
            switch (value)
            {
                case AxisLabelAlignmentOptions.End:
                    xAxisLabelLatexNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Left;
                    break;
                case AxisLabelAlignmentOptions.Along:
                    xAxisLabelLatexNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
                    break;
                default:
                    GD.PrintErr("Unrecognized axis label alignment option.");
                    break;
            }
        }
    }
    public float XAxisLabelOffset = 1;
    public float XAxisLabelScale = 1;
    private string xAxisLabel = "";
    private LatexNode xAxisLabelLatexNode;
    public string XAxisLabel
    {
        get => xAxisLabel;
        set
        {
            xAxisLabel = value;
            if (IsInstanceValid(xAxisLabelLatexNode)) xAxisLabelLatexNode.Free();
            xAxisLabelLatexNode = LatexNode.Create(value);

            if (XAxisAlignment == AxisLabelAlignmentOptions.End)
            {
                xAxisLabelLatexNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Left;
            }
            
            AddChild(xAxisLabelLatexNode);
        }
    }
    
    public AxisLabelAlignmentOptions YAxisAlignment = AxisLabelAlignmentOptions.End;
    public float YAxisLabelOffset = 1;
    public float YAxisLabelScale = 1;
    private string yAxisLabel = "";
    public LatexNode YAxisLabelLatexNode;
    public string YAxisLabel
    {
        get => yAxisLabel;
        set
        {
            yAxisLabel = value;
            YAxisLabelLatexNode = LatexNode.Create(value);
            AddChild(YAxisLabelLatexNode);
        }
    }

    /// <summary>
    /// Overload method with simplified duration argument.
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="transitionDataObjects"></param>
    /// <returns></returns>
    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        return Transition(duration, duration, duration);
    }

    /// <summary>
    /// The main implementation of Transition. It receives seperate duration arguments for the three stages of a graph
    /// transition.
    /// </summary>
    /// <param name="durationTuple"></param>
    /// <param name="addDuration"></param>
    /// <param name="transitionDataObjects"></param>
    /// <param name="removeDuration"></param>
    /// <param name="updateDuration"></param>
    /// <returns></returns>
    public Animation Transition( double removeDuration, double updateDuration, double addDuration)
    {
        var removeTransitions = new List<Animation>();
        var updateTransitions = new List<Animation>();
        var addTransitions = new List<Animation>();
        
        // Update data space markers
        // This happens during remove with zero (epsilon) duration, because the sooner the better.
        removeTransitions.Add(_dataSpaceMin.MoveTo(new Vector3(XAxis.Min, YAxis.Min, ZAxis.Min), duration: 0));
        removeTransitions.Add(_dataSpaceMax.MoveTo(new Vector3(XAxis.Max, YAxis.Max, ZAxis.Max), duration: 0));

        foreach (var axis in Axes)
        {
            var (remove, update, add) = axis.UpdateChildren();
            removeTransitions.Add(remove);
            updateTransitions.Add(update);
            addTransitions.Add(add);
        }

        // This condition is meant to avoid messing up tweened data updates
        // But actually I think we never want to mix those. Commenting out for now.
        // if (transitionDataObjects)
        // {
            updateTransitions.AddRange(
                GetChildren().OfType<IPrimerGraphData>().Select(x => x.Transition(updateDuration))
            );
        // }
        
        // Axis labels
        if (xAxisLabelLatexNode is not null)
        {
            updateTransitions.Add(
                xAxisLabelLatexNode.MoveTo(
                    XAxisAlignment == AxisLabelAlignmentOptions.Along
                        ? new Vector3(XAxis.LengthMinusPadding / 2, -XAxisLabelOffset, 0)
                        : new Vector3(XAxis.LengthMinusPadding + XAxisLabelOffset, 0, 0)
                )
            );
            updateTransitions.Add(        
                xAxisLabelLatexNode.ScaleTo(Vector3.One * XAxisLabelScale)
            );
        }
        if (YAxisLabelLatexNode is not null)
        {
            updateTransitions.Add(
                YAxisLabelLatexNode.MoveTo(
                    YAxisAlignment == AxisLabelAlignmentOptions.Along
                        ? new Vector3(-YAxisLabelOffset, YAxis.LengthMinusPadding / 2, 0)
                        : new Vector3(0, YAxis.LengthMinusPadding + YAxisLabelOffset, 0)
                )
            );
            updateTransitions.Add(
                YAxisLabelLatexNode.RotateTo(
                    YAxisAlignment == AxisLabelAlignmentOptions.Along
                        ? new Vector3(0, 0, 90)
                        : Vector3.Zero
                )
            );
            updateTransitions.Add(        
                YAxisLabelLatexNode.ScaleTo(Vector3.One * YAxisLabelScale)
            );
        }
        
        return AnimationUtilities.Series(
            removeTransitions.InParallel().WithDuration(removeDuration),
            updateTransitions.InParallel().WithDuration(updateDuration),
            addTransitions.InParallel().WithDuration(addDuration)
        );
    }

    // public Tween ShrinkPlottedDataToEnd()
    // {
    //     return GetComponentsInChildren<IPrimerGraphData>().Select(x => x.ShrinkToEnd()).RunInParallel();
    // }
    //
    // // We assume there will be no data present when the axes are drown, so they appear independently
    // public Tween Appear() => Axes.Select(x => x.Appear()).RunInParallel();
    // public Tween Disappear() => Axes.Select(x => x.Disappear()).RunInParallel();
    
    public CurvePlot2D AddCurvePlot2D(string name = "Curve")
    {
        var line = new CurvePlot2D();
        line.TransformPointFromDataSpaceToPositionSpace = GetDataSpaceToPositionSpaceFromSettings;
        AddChild(line);
        line.Owner = GetTree().EditedSceneRoot;
        line.Name = name;
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
    
    public BarPlot AddBarPlot(string name = "BarPlot")
    {
        var barPlot = new BarPlot();
        barPlot.TransformPointFromDataSpaceToPositionSpace = GetDataSpaceToPositionSpaceFromSettings;
        AddChild(barPlot);
        barPlot.Owner = GetTree().EditedSceneRoot;
        barPlot.Name = name;
        return barPlot;
    }

    public BarPlot3D AddBarPlot3D(string name = "BarPlot3D")
    {
        var barPlot = new BarPlot3D();
        barPlot.TransformPointFromDataSpaceToPositionSpace = GetDataSpaceToPositionSpaceFromSettings;
        AddChild(barPlot);
        barPlot.Owner = GetTree().EditedSceneRoot;
        barPlot.Name = name;
        return barPlot;
    }

    public StackedBarPlot AddStackedBarPlot(string name = "StackedBarPlot")
    {
        var stackedBarPlot = new StackedBarPlot();
        stackedBarPlot.TransformPointFromDataSpaceToPositionSpace = GetDataSpaceToPositionSpaceFromSettings;
        AddChild(stackedBarPlot);
        stackedBarPlot.Owner = GetTree().EditedSceneRoot;
        stackedBarPlot.Name = name;
        return stackedBarPlot;
    }
    
    public Vector3 GetDataSpaceToPositionSpaceFromSettings(Vector3 point)
    {
        return new Vector3(
            (point.X - XAxis.Min) / XAxis.RangeSize * XAxis.LengthMinusPadding,
            (point.Y - YAxis.Min) / YAxis.RangeSize * YAxis.LengthMinusPadding,
            (point.Z - ZAxis.Min) / ZAxis.RangeSize * ZAxis.LengthMinusPadding
        );
    }

    public Vector3 GetDataSpaceToPositionSpaceFromCurrentObjects(Vector3 point)
    {
        var min = _dataSpaceMin.Position;
        var max = _dataSpaceMax.Position;
        var range = max - min;

        return new Vector3(
            (point.X - min.X) / range.X * XAxis.LengthMinusPadding,
            (point.Y - min.Y) / range.Y * YAxis.LengthMinusPadding,
            (point.Z - min.Z) / range.Z * ZAxis.LengthMinusPadding
        );
    }
}
