using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerAssets;
using PrimerTools.LaTeX;

namespace PrimerTools.Graph;

public partial class TernaryGraph : Node3D
{
    private float _pointSizeFactor = 3;
    public string[] LabelStrings = {"A", "B", "C"};
    public LatexNode[] Labels = new LatexNode[3];
    public float LabelScale = 0.1f;
    public MeshInstance3D[] BoundingObjects = new MeshInstance3D[6];
    
    public Color[] Colors = {new Color(1, 0, 0), new Color(0, 1, 0), new Color(0, 0, 1)}; 
    
    public void CreateBounds(float chonk = 0.01f)
    {
        var corners = new Vector3[]
        {
            new (0, 0, 0),
            new (1, 0, 0),
            new (0.5f, Mathf.Sqrt(3) / 2, 0)
        };

        var center = (corners[0] + corners[1] + corners[2]) / 3;
        var cornersToCenterNormalized = corners.Select(x => (center - x).Normalized()).ToArray();
        var correctedCorners = new Vector3[]
        {
            corners[0] + (cornersToCenterNormalized[1] + cornersToCenterNormalized[2]) * chonk * 2,
            corners[1] + (cornersToCenterNormalized[2] + cornersToCenterNormalized[0]) * chonk * 2,
            corners[2] + (cornersToCenterNormalized[0] + cornersToCenterNormalized[1]) * chonk * 2
        };

        var cylinderMat = new StandardMaterial3D();
        cylinderMat.AlbedoColor = new Color(1, 1, 1);

        for (var i = 0; i < 3; i++)
        {
            // Cylinder
            var cylinder = new MeshInstance3D();
            cylinder.Name = $"Cylinder {i}";
            AddChild(cylinder);
            cylinder.Owner = GetTree().EditedSceneRoot;

            var mesh = new CylinderMesh();
            mesh.Height = 1;
            mesh.BottomRadius = chonk;
            mesh.TopRadius = chonk;
            mesh.SurfaceSetMaterial(0, cylinderMat);
            cylinder.Mesh = mesh;
            
            cylinder.Position = (correctedCorners[i] + correctedCorners[(i + 1) % 3]) / 2;
            cylinder.RotationDegrees = new Vector3(0, 0, 90 + 120 * i);

            BoundingObjects[2 * i] = cylinder;
            
            // Sphere
            var sphere = new MeshInstance3D();
            sphere.Name = $"Sphere {i}";
            
            AddChild(sphere);
            sphere.Owner = GetTree().EditedSceneRoot;
            
            var sMesh = new SphereMesh();
            var mat = new StandardMaterial3D();
            mat.AlbedoColor = Colors[i];
            sMesh.SurfaceSetMaterial(0, mat);
            sMesh.Height = 2 * chonk * _pointSizeFactor;
            sMesh.Radius = chonk * _pointSizeFactor;
            sphere.Mesh = sMesh;
            
            sphere.Position = correctedCorners[i];
            BoundingObjects[2 * i + 1] = sphere;
        }
        
        CreateLabels(chonk);
    }

    /// <summary>
    /// Used to be part of CreateBounds, but I wanted one with just labels. So this is hacked.
    /// It's still called from CreateBounds to avoid breaking old scenes.
    /// </summary>
    public void CreateLabels(float chonk = 0.01f)
    {
        // This part is duplicated in CreateBounds
        var corners = new Vector3[]
        {
            new (0, 0, 0),
            new (1, 0, 0),
            new (0.5f, Mathf.Sqrt(3) / 2, 0)
        };

        var center = (corners[0] + corners[1] + corners[2]) / 3;
        var cornersToCenterNormalized = corners.Select(x => (center - x).Normalized()).ToArray();
        var correctedCorners = new Vector3[]
        {
            corners[0] + (cornersToCenterNormalized[1] + cornersToCenterNormalized[2]) * chonk * 2,
            corners[1] + (cornersToCenterNormalized[2] + cornersToCenterNormalized[0]) * chonk * 2,
            corners[2] + (cornersToCenterNormalized[0] + cornersToCenterNormalized[1]) * chonk * 2
        };
        // End duplicated part
        
        for (var i = 0; i < 3; i++)
        {
            // Label
            var label = new LatexNode();
            AddChild(label);
            label.Owner = GetTree().EditedSceneRoot;

            label.latex = LabelStrings[i];
            label.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
            label.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;
            label.UpdateCharacters();
            label.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);

            Vector3 offset;
            if (i < 2) offset = new Vector3(0, -0.16f, 0);
            else offset = new Vector3(0, 0.15f, 0);
            
            label.Position = correctedCorners[i] + offset;
            label.Scale = new Vector3(LabelScale, LabelScale, LabelScale);
            label.Name = LabelStrings[i];
            Labels[i] = label;
        }
    }

    public Animation ScaleBoundingObjectsUpFromZero()
    {
        return AnimationUtilities.Series(
            BoundingObjects.Select(x => x.ScaleTo(0)).InParallel().WithDuration(AnimationUtilities.TimeEpsilon),
            BoundingObjects.Select(x => x.ScaleTo(1)).InParallel()
        );
    }
    public Animation ScaleBoundingObjectsDownToZero()
    {
        return AnimationUtilities.Series(
            BoundingObjects.Select(x => x.ScaleTo(1)).InParallel().WithDuration(AnimationUtilities.TimeEpsilon),
            BoundingObjects.Select(x => x.ScaleTo(0)).InParallel()
        );
    }

    public Animation ScaleLabelsUpFromZero()
    {
        return AnimationUtilities.Series(
            Labels.Select(x => x.ScaleTo(0)).InParallel().WithDuration(AnimationUtilities.TimeEpsilon),
            Labels.Select(x => x.ScaleTo(LabelScale)).InParallel()
        );
    }
    public Animation ScaleLabelsDownToZero()
    {
        return AnimationUtilities.Series(
            Labels.Select(x => x.ScaleTo(LabelScale)).InParallel().WithDuration(AnimationUtilities.TimeEpsilon),
            Labels.Select(x => x.ScaleTo(0)).InParallel()
        );
    }
    
    public static Vector3 CoordinatesToPosition(float a, float b, float c)
    {
        ValidateCoordinates(a, b, c);
        return new Vector3(
            b + c / 2,
            c * Mathf.Sqrt(3) / 2,
            0
        );
    }
    
    public static Vector3 CoordinatesToPosition(Vector3 point)
    {
        return CoordinatesToPosition(point.X, point.Y, point.Z);
    }
    public static Vector3 CoordinatesToPositionButXAndYAreTheOnesThatMatter(Vector3 point)
    {
        return CoordinatesToPosition(0, point.X, point.Y);
    }

    private static void ValidateCoordinates(params float[] coords)
    {
        var sum = coords.Sum();

        if (Mathf.Abs(sum - 1) > 0.00001f)
            GD.PushWarning($"Sum of coordinates must be 1 but it's {sum}");

        if (coords.Any(x => x < 0))
            GD.PushWarning($"Coordinates must all be positive, but they are {coords.Join()}");
    }

    public delegate void TernaryMethod(float a, float b, float c, int index);
    public static void IterateOverTriangleWithNumUnitsPerSide(int unitsPerSide, TernaryMethod ternaryMethod)
    {
        var increment = 1f / (unitsPerSide - 1);
        var k = 0;
        for (var i = 0; i <= unitsPerSide; i++)
        {
            for (var j = 0; j < unitsPerSide; j++)
            {
                if (j + i >= unitsPerSide) continue;
                var a = (unitsPerSide - 1 - i - j) * increment ;
                var b = i * increment;
                var c = j * increment;

                ternaryMethod(a, b, c, k);

                k++;
            }
        }
    }
}
