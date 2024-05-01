using System.Linq;
using Godot;
using PrimerAssets;

namespace PrimerTools.Graph;

public partial class TernaryGraphWithBars : TernaryGraph
{
    [Export] public int BarsPerSide = 10;

    private PackedScene hexagonalPrismScene =
        ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Graph/hexagonal_prism.tscn");

    public float[] Data;
    
    public void AddBars()
    {
        var increment = 1f / (BarsPerSide - 1);
        var k = 0;
        for (var i = 0; i <= BarsPerSide; i++)
        {
            for (var j = 0; j < BarsPerSide; j++)
            {
                if (j + i >= BarsPerSide) continue;
                
                var bar = MakeBar();
                AddChild(bar);
                
                var a = (BarsPerSide - 1 - i - j) * increment ;
                var b = i * increment;
                var c = j * increment;
                
                bar.Position = CoordinatesToPosition(a, b, c);

                var mat = new StandardMaterial3D();
                mat.AlbedoColor = PrimerColor.MixColorsByWeight(
                    colors: Colors.ToArray(),
                    new []{ a, b, c },
                    subtractive: true,
                    messWithBrightness: true
                );

                bar.GetNode<MeshInstance3D>("Cylinder").Mesh.SurfaceSetMaterial(0, mat); 
                bar.RotationDegrees = new Vector3(90, 0, 0);
                
                // We need to fit 1 less than the number of bars into the range from 0 to 1
                // Also, 1 is the default diameter from corner to corner in the cylinder mesh, 
                // So need to do the sqrt 3 thing to make the sides touch.
                var widthScale = 1f / (BarsPerSide - 1) / (Mathf.Sqrt(3) / 2) / 2;
                bar.Scale = new Vector3(widthScale, Data[k], widthScale);

                k++;
            }
        }
    }
    
    private Node3D MakeBar()
    {
        var bar = hexagonalPrismScene.Instantiate<Node3D>();
        var meshInstance3D = bar.GetNode<MeshInstance3D>("Cylinder");
        var newMesh = (Mesh)bar.GetNode<MeshInstance3D>("Cylinder").Mesh.Duplicate();
        meshInstance3D.Mesh = newMesh;

        return bar;
    }
}