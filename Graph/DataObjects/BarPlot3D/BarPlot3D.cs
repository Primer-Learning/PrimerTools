using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public partial class BarPlot3D : Node3D, IPrimerGraphData
{
    private int BinsX => _data.GetLength(0);
    private int BinsY => _data.GetLength(1);
    private float[,] _data = new float[0, 0];
    
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    
    public delegate float[,] DataFetch();
    public DataFetch DataFetchMethod;

    public Color[,] Colors = new Color[,]
    {
        { PrimerColor.Blue, PrimerColor.Red },
        { PrimerColor.Red, PrimerColor.Blue }
    };
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
    }
    
    public void SetData(float[,] bars)
    {
        _data = bars;
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
        
        bar.Position = CalculateBarPosition(x, y);
        bar.Scale = CalculateBarScale(0);
        
        return bar;
    }

    public Node3D GetBar(int x, int y)
    {
        return GetNodeOrNull<Node3D>($"Bar {x}, {y}");
    }

    private Vector3 CalculateBarPosition(int x, int y)
    {
        return TransformPointFromDataSpaceToPositionSpace(
            new Vector3(
                (x + 0.5f) * BarWidth,
                0,
                (y + 0.5f) * BarDepth
            )
        );
    }

    private Vector3 CalculateBarScale(float data)
    {
        return TransformPointFromDataSpaceToPositionSpace(
            new Vector3(
                BarWidth,
                Math.Max(data, 0.001f), // Ensure some minimum height
                BarDepth
            )
        );
    }
    
    public Node3D EnsureBarExists(int x, int y)
    {
        var bar = GetBar(x, y);
        
        if (bar != null) return bar;
        
        bar = CreateBar(x, y);

        // Set initial material
        var meshInstance = bar.GetNode<MeshInstance3D>("MeshInstance3D");
        var material = new StandardMaterial3D();
        material.AlbedoColor = Colors[x % Colors.GetLength(0), y % Colors.GetLength(1)];
        meshInstance.Mesh.SurfaceSetMaterial(0, material);
        
        return bar;
    }

    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        var animations = new List<Animation>();
        
        for (var x = 0; x < BinsX; x++)
        for (var y = 0; y < BinsY; y++)
        {
            var bar = EnsureBarExists(x, y);
            
            // Position the bar
            animations.Add(bar.MoveToAnimation(CalculateBarPosition(x, y)));
            
            // Scale the bar
            animations.Add(bar.ScaleToAnimation(CalculateBarScale(_data[x, y])));
        }

        return animations.InParallel().WithDuration(duration);
    }

    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        if (_data.GetLength(0) == 0) return null;
        
        var tween = CreateTween();
        tween.SetParallel();

        // GD.Print($"Data dimensions: {_data.GetLength(0)} by {_data.GetLength(1)}");
        
        for (var x = 0; x < BinsX; x++)
        for (var y = 0; y < BinsY; y++)
        {
            var bar = EnsureBarExists(x, y);
            
            // Position the bar
            tween.TweenProperty(
                bar,
                "position",
                CalculateBarPosition(x, y),
                duration
            );
            // Scale the bar
            tween.TweenProperty(
                bar,
                "scale",
                CalculateBarScale(_data[x, y]),
                duration
            );
        }
        
        return tween;
    }

    public Animation Disappear()
    {
        var animations = new List<Animation>();
        
        for (var x = 0; x < BinsX; x++)
        for (var y = 0; y < BinsY; y++)
        {
            var bar = GetBar(x, y);
            if (bar == null) continue;
            animations.Add(bar.ScaleToAnimation(Vector3.Zero));
        }

        return animations.InParallel();
    }
}
