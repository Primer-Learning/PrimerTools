using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerAssets;

namespace PrimerTools.Graph;

public partial class BarPlot3D : Node3D, IPrimerGraphData
{
    private int BinsX => _data.GetLength(0);
    private int BinsY => _data.GetLength(0);
    private float[,] _data;
    private Node3D[,] _bars;
    
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    
    public delegate float[,] DataFetch();
    public DataFetch DataFetchMethod;
    
    public Color[] Colors = PrimerColor.Rainbow.ToArray();
    public float BarWidth { get; set; } = 1;
    public float BarDepth { get; set; } = 1;

    public void FetchData()
    {
        if (DataFetchMethod == null)
        {
            GD.PrintErr("DataFetchMethod not assigned");
            return;
        }
        
        _data = DataFetchMethod();
        _bars = new Node3D[BinsX, BinsY];
    }

    private Node3D CreateBar(int x, int y)
    {
        var bar = new Node3D();
        bar.Name = $"Bar {x}, {y}";
        AddChild(bar);
        var meshInstance = new MeshInstance3D();
        meshInstance.Name = "MeshInstance3D";
        bar.AddChild(meshInstance);
        bar.MakeSelfAndChildrenLocal();
        
        var boxMesh = new BoxMesh();
        boxMesh.Size = Vector3.One;
        meshInstance.Mesh = boxMesh;
        meshInstance.Position = Vector3.Up * 0.5f; // Put the base of the bar at the parent's pivot point 
        
        return bar;
    }

    private void EnsureBarExists(int x, int y)
    {
        if (_bars[x, y] != null) return;
        
        var bar = CreateBar(x, y);
        _bars[x, y] = bar;
        
        // Set initial material
        var meshInstance = bar.GetNode<MeshInstance3D>("MeshInstance3D");
        var material = new StandardMaterial3D();
        material.AlbedoColor = Colors[(x + y) % Colors.Length];
        meshInstance.Mesh.SurfaceSetMaterial(0, material);
    }

    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        var animations = new List<Animation>();
        
        for (var x = 0; x < BinsX; x++)
        for (var y = 0; y < BinsY; y++)
        {
            EnsureBarExists(x, y);
            var bar = _bars[x, y];
            
            // Position the bar
            var targetPosition = new Vector3(
                (x + 0.5f) * BarWidth,
                0, // Center of bar
                (y + 0.5f) * BarDepth
            );
            animations.Add(bar.MoveTo(TransformPointFromDataSpaceToPositionSpace(targetPosition)));
            
            // Scale the bar
            var targetScale = new Vector3(
                BarWidth,
                Math.Max(_data[x, y], 0.001f), // Ensure some minimum height
                BarDepth
            );
            animations.Add(bar.ScaleTo(TransformPointFromDataSpaceToPositionSpace(targetScale)));
        }

        return animations.RunInParallel().WithDuration(duration);
    }

    public Animation Disappear()
    {
        var animations = new List<Animation>();
        
        for (var x = 0; x < BinsX; x++)
        for (var y = 0; y < BinsY; y++)
        {
            if (_bars[x, y] == null) continue;
            animations.Add(_bars[x, y].ScaleTo(Vector3.Zero));
        }

        return animations.RunInParallel();
    }
}
