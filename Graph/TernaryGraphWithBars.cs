using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public partial class TernaryGraphWithBars : TernaryGraph
{
    [Export] public int BarsPerSide = 10;

    private PackedScene hexagonalPrismScene =
        ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/hexagonal_prism.tscn");

    public float[] Data;
    private Node3D[] bars;
    
    // We need to fit 1 less than the number of bars into the range from 0 to 1
    // Also, 1 is the default diameter from corner to corner in the cylinder mesh, 
    // So need to do the sqrt 3 thing to make the sides touch.
    private float BarWidthScale => 1f / (BarsPerSide - 1) / (Mathf.Sqrt(3) / 2) / 2;
    public int TotalNumberOfBars => BarsPerSide * (BarsPerSide + 1) / 2;

    private void MakeBarsConsideringTheParameters(float a, float b, float c, int k)
    {
        bars ??= new Node3D[TotalNumberOfBars];
        
        Node3D bar;
        if (bars[k] is not null)
        {
            bar = bars[k];
        }
        else
        {
            bar = MakeBar();
            AddChild(bar);
            bars[k] = bar;
        }
        
        bar.Position = CoordinatesToPosition(a, b, c);
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = PrimerColor.MixColorsByWeight(
            colors: Colors.ToArray(),
            new []{ a, b, c }
            // subtractive: true,
            // messWithBrightness: true
        );

        bar.GetNode<MeshInstance3D>("Cylinder").Mesh.SurfaceSetMaterial(0, mat); 
        bar.RotationDegrees = new Vector3(90, 0, 0);
        
        bar.Scale = Vector3.Zero; //new Vector3(widthScale, Data[k], widthScale);
    }
    
    public void AddBars()
    {
        IterateOverTriangleWithNumUnitsPerSide(BarsPerSide, MakeBarsConsideringTheParameters);
    }
    
    private Node3D MakeBar()
    {
        var bar = hexagonalPrismScene.Instantiate<Node3D>();
        var meshInstance3D = bar.GetNode<MeshInstance3D>("Cylinder");
        var newMesh = (Mesh)bar.GetNode<MeshInstance3D>("Cylinder").Mesh.Duplicate();
        meshInstance3D.Mesh = newMesh;

        return bar;
    }

    public Animation Transition()
    {
        var animations = new List<Animation>();
        for (var i = 0; i < TotalNumberOfBars; i++)
        {
            animations.Add(bars[i].ScaleTo( new Vector3(BarWidthScale, Mathf.Max(Data[i], 0.001f), BarWidthScale)));
        }
        return animations.InParallel();
    }
}