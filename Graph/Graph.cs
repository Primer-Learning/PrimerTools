using Godot;
using System.Collections.Generic;
using System.Linq;
using PrimerTools.LaTeX;
using PrimerTools.TweenSystem;

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
    
    public Graph()
    {
        Name = "Graph";
        InstantiateAxes();
        
        // Initialize data space marker nodes
        _dataSpaceMin = new Node3D { Name = "DataSpaceMin" };
        _dataSpaceMax = new Node3D { Name = "DataSpaceMax" };
        AddChild(_dataSpaceMin);
        AddChild(_dataSpaceMax);
    }
    
    private void InstantiateAxes()
    {
        var axisScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis.tscn");
        var x = axisScene.Instantiate<Axis>();
        var y = axisScene.Instantiate<Axis>();
        var z = axisScene.Instantiate<Axis>();
        x.Type = Axis.AxisType.X;
        x.TicScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis_tic_x.tscn");
        y.Type = Axis.AxisType.Y;
        y.TicScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/Scenes/axis_tic_y.tscn");
        y.RotationDegrees = new Vector3(0, 0, 90);
        z.Type = Axis.AxisType.Z;
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

    // Proxy properties for backward compatibility (axis labels are controlled by axes now)
    public AxisLabelAlignmentOptions XAxisLabelAlignment
    {
        get => XAxis.LabelAlignment;
        set => XAxis.LabelAlignment = value;
    }
    public float XAxisLabelOffset
    {
        get => XAxis.LabelOffset;
        set => XAxis.LabelOffset = value;
    }
    public float XAxisLabelScale
    {
        get => XAxis.LabelScale;
        set => XAxis.LabelScale = value;
    }
    public string XAxisLabel
    {
        get => XAxis.Label;
        set => XAxis.Label = value;
    }
    
    public AxisLabelAlignmentOptions YAxisLabelAlignment
    {
        get => YAxis.LabelAlignment;
        set => YAxis.LabelAlignment = value;
    }
    public float YAxisLabelOffset
    {
        get => YAxis.LabelOffset;
        set => YAxis.LabelOffset = value;
    }
    public float YAxisLabelScale
    {
        get => YAxis.LabelScale;
        set => YAxis.LabelScale = value;
    }
    public string YAxisLabel
    {
        get => YAxis.Label;
        set => YAxis.Label = value;
    }
    
    public AxisLabelAlignmentOptions ZAxisLabelAlignment
    {
        get => ZAxis.LabelAlignment;
        set => ZAxis.LabelAlignment = value;
    }
    public float ZAxisLabelOffset
    {
        get => ZAxis.LabelOffset;
        set => ZAxis.LabelOffset = value;
    }
    public float ZAxisLabelScale
    {
        get => ZAxis.LabelScale;
        set => ZAxis.LabelScale = value;
    }
    public string ZAxisLabel
    {
        get => ZAxis.Label;
        set => ZAxis.Label = value;
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

    // TODO: Fix the convenient Transition method, making it give zero duration to portions with no real animations.
    // Currently, this serves as a workaround for that case, but its purpose is to give fine control.
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
        removeTransitions.Add(_dataSpaceMin.MoveToAnimation(new Vector3(XAxis.Min, YAxis.Min, ZAxis.Min), duration: 0));
        removeTransitions.Add(_dataSpaceMax.MoveToAnimation(new Vector3(XAxis.Max, YAxis.Max, ZAxis.Max), duration: 0));

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
        
        return AnimationUtilities.Series(
            removeTransitions.InParallel().WithDuration(removeDuration),
            updateTransitions.InParallel().WithDuration(updateDuration),
            addTransitions.InParallel().WithDuration(addDuration)
        );
    }
    
    public CompositeStateChange TransitionStateChange(double duration = AnimationUtilities.DefaultDuration)
    {
        return TransitionStateChange(duration, duration, duration);
    }

    public CompositeStateChange TransitionStateChange(double removeDuration, double updateDuration, double addDuration)
    {
        var composite = new CompositeStateChange().WithName("Graph Transition");

        // Phase containers
        var removePhase = new CompositeStateChange().WithName("Remove Phase");
        var updatePhase = new CompositeStateChange().WithName("Update Phase");
        var addPhase = new CompositeStateChange().WithName("Add Phase");

        // Update data space markers (happens during remove with zero duration)
        removePhase.AddStateChange(_dataSpaceMin.MoveTo(new Vector3(XAxis.Min, YAxis.Min, ZAxis.Min)).WithDuration(0));
        removePhase.AddStateChange(_dataSpaceMax.MoveTo(new Vector3(XAxis.Max, YAxis.Max, ZAxis.Max)).WithDuration(0));

        // Axis processing
        foreach (var axis in Axes)
        {
            var axisTransition = axis.UpdateChildrenStateChange();
            removePhase.AddStateChangeInParallel(axisTransition.Remove);
            updatePhase.AddStateChangeInParallel(axisTransition.Update);
            addPhase.AddStateChangeInParallel(axisTransition.Add);
        }
        
        // Update data objects
        foreach (var dataObject in GetChildren().OfType<IPrimerGraphData>())
        {
            updatePhase.AddStateChangeInParallel(dataObject.TransitionStateChange(updateDuration));
        }
        
        composite.AddStateChange(removePhase.WithDuration(removeDuration));
        composite.AddStateChange(updatePhase.WithDuration(updateDuration));
        composite.AddStateChange(addPhase.WithDuration(addDuration));

        return composite;
    }
    
    public CurvePlot2D AddCurvePlot2D(string name = "Curve")
    {
        var line = new CurvePlot2D();
        line.TransformPointFromDataSpaceToPositionSpace = GetDataSpaceToPositionSpaceFromSettings;
        AddChild(line);
        line.Position = new Vector3(
            XAxis.Min * XAxis.DataSpaceScale,
            YAxis.Min * YAxis.DataSpaceScale,
            ZAxis.Min * ZAxis.DataSpaceScale
        );
        if (Engine.IsEditorHint())
        {
            line.Owner = GetTree().EditedSceneRoot;
        }
        line.Name = name;
        return line;
    }
    
    public SurfacePlot AddSurfacePlot(string name = "SurfacePlot")
    {
        var surfacePlot = new SurfacePlot();
        surfacePlot.TransformPointFromDataSpaceToPositionSpace = GetDataSpaceToPositionSpaceFromSettings;
        AddChild(surfacePlot);
        surfacePlot.Position = new Vector3(
            XAxis.Min * XAxis.DataSpaceScale,
            YAxis.Min * YAxis.DataSpaceScale,
            ZAxis.Min * ZAxis.DataSpaceScale
        );
        surfacePlot.Owner = GetTree().EditedSceneRoot;
        surfacePlot.Name = name;
        return surfacePlot;
    }
    
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

    public void AddChildInDataSpace(Node3D newChild, Vector3 dataPosition)
    {
        AddChild(newChild);
        newChild.Position = GetDataSpaceToPositionSpaceFromCurrentObjects(dataPosition);
    }
}
