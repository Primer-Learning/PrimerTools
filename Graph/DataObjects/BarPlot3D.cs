using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerAssets;

namespace PrimerTools.Graph;

public partial class BarPlot3D : Node3D, IPrimerGraphData
{
    public int BinsX { get; }
    public int BinsY { get; }
    private float[,] _data;
    private Node3D[,] _bars;
    
    public delegate float[,] DataFetch();
    public DataFetch DataFetchMethod;
    
    public Color[] Colors = PrimerColor.Rainbow.ToArray();
    public float BarWidth { get; set; } = 1;
    public float BarDepth { get; set; } = 1;

    public BarPlot3D(int binsX = 10, int binsY = 10, string name = null)
    {
        BinsX = binsX;
        BinsY = binsY;
        _data = new float[BinsX, BinsY];
        _bars = new Node3D[BinsX, BinsY];
        
        if (name == null) Name = "BarPlot3D";
    }

    public void FetchData()
    {
        if (DataFetchMethod == null)
        {
            GD.PrintErr("DataFetchMethod not assigned");
            return;
        }
        
        _data = DataFetchMethod();
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
        boxMesh.Size = Vector3.One; // Will be scaled to actual size
        meshInstance.Mesh = boxMesh;
        
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
                x * BarWidth,
                _data[x, y] / 2, // Center of bar
                y * BarDepth
            );
            animations.Add(bar.MoveTo(targetPosition));
            
            // Scale the bar
            var targetScale = new Vector3(
                BarWidth * 0.9f, // Slight gap between bars
                Math.Max(_data[x, y], 0.001f), // Ensure some minimum height
                BarDepth * 0.9f
            );
            animations.Add(bar.ScaleTo(targetScale));
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
