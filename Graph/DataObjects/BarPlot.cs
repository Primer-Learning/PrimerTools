using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Primer;
using PrimerTools.LaTeX;

namespace PrimerTools.Graph;

public partial class BarPlot : Node3D, IPrimerGraphData
{
    public Color[] Colors = PrimerColor.rainbow.ToArray();
    
    public bool ShowValuesOnBars = false;
    public float BarLabelScaleFactor = 1;
    public float BarLabelVerticalOffset = 0.5f;
    public string BarLabelPrefix = "";
    public string BarLabelSuffix = "";
    public int BarLabelDecimalPlaces = 0;
    private Vector3 parentGraphSize => new Vector3(GetParent<Graph>().XAxis.length, GetParent<Graph>().YAxis.length, GetParent<Graph>().ZAxis.length);
    
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    
    private List<Tuple<float, float, float>> renderedRectProperties = new();
    public List<float> Data;

    private List<Tuple<float, float, float>> DataAsRectProperties()
    {
        return Data.Select( (value, i) =>
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

    public Animation Transition(float duration = AnimationUtilities.DefaultDuration)
    {
        var trackCount = 0;

        var rectProperties = DataAsRectProperties();

        var animations = new List<Animation>();

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
                bar.Position = new Vector3(rectProperties[i].Item1, 0, 0);
                bar.Mesh = mesh;
                bar.Name = $"Bar {i}";
                AddChild(bar);
                bar.Owner = GetTree().EditedSceneRoot;
                var newMat = new StandardMaterial3D();
                newMat.AlbedoColor = Colors[i % Colors.Length];
                bar.Mesh.SurfaceSetMaterial(0, newMat);

                if (ShowValuesOnBars)
                {
                    // TODO: Consider not using LaTeX here. It's overkill, but is currently more work
                    // at time of writing.
                    var label = new LatexNode();
                    label.latex = 0.ToString();
                    label.Name = $"Label {i}";
                    label.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
                    label.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Bottom;
                    label.UpdateCharacters();
                    AddChild(label);
                    label.Owner = GetTree().EditedSceneRoot;
                    label.Position = bar.Position + Vector3.Up * BarLabelVerticalOffset * parentGraphSize.Y / 10;
                    label.Scale = Vector3.Zero;
                }
            }
            
            // Position animation
            var targetPosition = new Vector3(rectProperties[i].Item1, rectProperties[i].Item2 / 2, 0);
            animations.Add(bar.AnimateValue(targetPosition, "position"));
            // Height animation
            var targetHeight = rectProperties[i].Item2;
            animations.Add(bar.AnimateValue(targetHeight, "mesh:size:y"));
            // Width animation
            var targetWidth = rectProperties[i].Item3;
            animations.Add(bar.AnimateValue(targetWidth, "mesh:size:x"));
            
            if (ShowValuesOnBars)
            {
                var theLabel = GetNode<LatexNode>($"Label {i}");
                
                var targetLabelPos = new Vector3(rectProperties[i].Item1,
                    rectProperties[i].Item2 + BarLabelVerticalOffset * parentGraphSize.Y / 10, 0);
                animations.Add(theLabel.MoveTo(targetLabelPos));
            
                var targetLabelScale = BarLabelScaleFactor * rectProperties[i].Item3 / 2 * Vector3.One;
                animations.Add(theLabel.ScaleTo(targetLabelScale));

                // var labelTextAnimation = new Animation();
                theLabel.numberSuffix = BarLabelSuffix;
                theLabel.numberPrefix = BarLabelPrefix;
                theLabel.DecimalPlacesToShow = BarLabelDecimalPlaces;
                
                animations.Add(theLabel.AnimateNumericalExpression(Mathf.RoundToInt(Data[i])));
            }
        }

        return animations.RunInParallel();
    }

    public Animation Disappear()
    {
        throw new System.NotImplementedException();
    }
    
    public void SetData(params float[] bars)
    {
        Data = bars.ToList();
    }

    public void AddData(params float[] newData)
    {
        Data ??= new List<float>();
        Data.AddRange(newData);
    }
}