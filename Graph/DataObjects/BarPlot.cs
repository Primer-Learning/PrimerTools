using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Primer;

namespace PrimerTools.Graph;

public partial class BarPlot : Node3D, IPrimerGraphData
{
    public Color[] colors = PrimerColor.rainbow.ToArray();
    
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    
    private List<Tuple<float, float, float>> renderedRectProperties = new();
    public List<float> data;

    private List<Tuple<float, float, float>> DataAsRectProperties()
    {
        return data.Select( (value, i) =>
            new Tuple<float, float, float>(
                TransformPointFromDataSpaceToPositionSpace(new Vector3(i + offset, 0, 0)).X,
                TransformPointFromDataSpaceToPositionSpace(new Vector3(0, value, 0)).Y,
                TransformPointFromDataSpaceToPositionSpace(new Vector3(barWidth, 0, 0)).X
            )
        ).ToList();
    }
    
    private float offset = 1;
    private float barWidth = 0.8f;
    private float barDepth = 0.01f;
    
    public Animation Transition(float duration)
    {
        var animation = new Animation();
        var trackCount = 0;
        
        var rectProperties = DataAsRectProperties();
        
        // Iterate through the data points
        for (var i = 0; i < rectProperties.Count; i++)
        {
            var bar = GetNodeOrNull<MeshInstance3D>($"Bar {i}");
            // If the bar doesn't exist, make it
            if (bar == null)
            {
                bar = new MeshInstance3D();
                var mesh = new BoxMesh();
                mesh.Size = TransformPointFromDataSpaceToPositionSpace(new Vector3(barWidth, 0, barDepth));
                bar.Position = TransformPointFromDataSpaceToPositionSpace(new Vector3(i + offset, 0, 0));
                bar.Mesh = mesh;
                bar.Name = $"Bar {i}";
                AddChild(bar);
                bar.Owner = GetTree().EditedSceneRoot;
                var newMat = new StandardMaterial3D();
                newMat.AlbedoColor = colors[i % colors.Length];
                bar.Mesh.SurfaceSetMaterial(0, newMat);
            }
            
            // Create an animation for the bar to move to the new position/height/width
            
            // Position track
            var targetPosition = new Vector3(rectProperties[i].Item1, rectProperties[i].Item2 / 2, 0);
            animation.AddTrack(Animation.TrackType.Value);
            animation.TrackSetPath(trackCount, bar.GetPath()+ ":position");
            animation.TrackInsertKey(trackCount, 0, bar.Position);
            animation.TrackInsertKey(trackCount, duration, targetPosition);
            animation.TrackSetInterpolationType(trackCount, Animation.InterpolationType.Cubic);
            trackCount++;
            bar.Position = targetPosition;
            
            // Height track
            var targetHeight = rectProperties[i].Item2;
            animation.AddTrack(Animation.TrackType.Value);
            animation.TrackSetPath(trackCount, bar.GetPath()+ ":mesh:size:y");
            animation.TrackInsertKey(trackCount, 0, ((BoxMesh)bar.Mesh).Size.Y);
            animation.TrackInsertKey(trackCount, duration, targetHeight);
            animation.TrackSetInterpolationType(trackCount, Animation.InterpolationType.Cubic);
            trackCount++;
            
            // Width track
            var targetWidth = rectProperties[i].Item3;
            animation.AddTrack(Animation.TrackType.Value);
            animation.TrackSetPath(trackCount, bar.GetPath()+ ":mesh:size:x");
            animation.TrackInsertKey(trackCount, 0, ((BoxMesh)bar.Mesh).Size.X);
            animation.TrackInsertKey(trackCount, duration, targetWidth);
            animation.TrackSetInterpolationType(trackCount, Animation.InterpolationType.Cubic);
            trackCount++;
            ((BoxMesh)bar.Mesh).Size = new Vector3(targetWidth, targetHeight, ((BoxMesh)bar.Mesh).Size.Z);
        }

        return animation;
    }

    public Animation Disappear()
    {
        throw new System.NotImplementedException();
    }
    
    public void SetData(params float[] bars)
    {
        data = bars.ToList();
    }

    public void AddData(params float[] newData)
    {
        data ??= new List<float>();
        data.AddRange(newData);
    }
}