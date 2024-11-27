using System.Collections.Generic;
using Godot;

namespace PrimerTools.Graph;

public partial class PointData : Node3D, IPrimerGraphData
{
    private Graph Graph => GetParent<Graph>();
    private List<(Vector3 position, MeshInstance3D node)> pointObjects = new();
    
    public void AddPoints(params Vector3[] positions)
    {
        foreach (var pos in positions)
        {
            var point = new MeshInstance3D();
            point.Mesh = new SphereMesh();
            point.Scale = Vector3.One * 0.2f;
            point.Name = "Point";
            AddChild(point);
            point.Owner = GetTree().EditedSceneRoot;
            point.Position = Graph.DataSpaceToPositionSpace(pos);
            pointObjects.Add((pos, point));
        }
    }
    
    public void SetData(params Vector3[] data)
    {
        // TODO: Make this handle situations where the data is longer or shorter than the current data.
        // Might be better to have the data and object lists be separate and handle adding/removing point objects
        // during animations.
        for (var i = 0; i < data.Length; i++)
        {
            pointObjects[i] = (data[i], pointObjects[i].node);
        }
    }

    public void FetchData()
    {
        throw new System.NotImplementedException();
    }

    public Animation Transition(double duration)
    {
        var transitionAnimations = new List<Animation>();
        foreach (var (position, node) in pointObjects)
        {
            transitionAnimations.Add(node.MoveTo(Graph.DataSpaceToPositionSpace(position)));
        }

        return transitionAnimations.InParallel();
    }

    public Tween TweenTransition(double duration)
    {
        throw new System.NotImplementedException();
    }

    public Animation Disappear()
    {
        throw new System.NotImplementedException();
    }
}