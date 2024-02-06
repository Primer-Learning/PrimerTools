using Godot;
using System.Collections.Generic;
namespace PrimerTools.Graph;

[Tool]
public partial class Graph : Node3D
{
    private MemberChangeChecker memberChangeChecker;
    public override void _Process(double delta)
    {
        memberChangeChecker ??= new MemberChangeChecker(this);
        if (Engine.IsEditorHint() && memberChangeChecker.CheckForChanges())
        {
            Update();
        }
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

    public void Update()
    {
        foreach (var axis in Axes)
        {
            axis.UpdateChildren();
        }
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
    
    // public PrimerLine AddLine(string name, Color color)
    // {
    //     var gnome = new SimpleGnome(transform);
    //     var line = gnome.Add<PrimerLine>(name);
    //     line.SetColor(color);
    //     line.transformPointFromDataSpaceToPositionSpace = DataSpaceToPositionSpace;
    //     line.Reset();
    //     return line;
    // }
    // public PrimerLine AddLine(string name)
    // {
    //     return AddLine(name, PrimerColor.white);
    // }
    //
    // public StackedArea AddStackedArea(string name)
    // {
    //     var gnome = new SimpleGnome(transform);
    //     var area = gnome.Add<StackedArea>(name);
    //     area.Reset(); // In case it already existed
    //     area.transformPointFromDataSpaceToPositionSpace = DataSpaceToPositionSpace;
    //     return area;
    // }
    //
    // public BarPlot AddBarPlot(string name)
    // {
    //     var gnome = new Primer.SimpleGnome(transform);
    //     var barPlot = gnome.Add<BarPlot>(name);
    //     barPlot.transformPointFromDataSpaceToPositionSpace = DataSpaceToPositionSpace;
    //     return barPlot;
    // }
    
    public Vector3 DataSpaceToPositionSpace(Vector3 point)
    {
        return new Vector3(
            (point.X - XAxis.min) / XAxis.RangeSize * XAxis.lengthMinusPadding,
            (point.Y - YAxis.min) / YAxis.RangeSize * YAxis.lengthMinusPadding,
            (point.Z - ZAxis.min) / ZAxis.RangeSize * ZAxis.lengthMinusPadding
        );
    }
    
    
}
public interface IPrimerGraphData
{
    public Tween Transition();
    public Tween ShrinkToEnd();
}
