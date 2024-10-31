using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerAssets;
using PrimerTools.LaTeX;

namespace PrimerTools.Graph;

public partial class StackedBarPlot : Node3D, IPrimerGraphData
{
    public Color[][] ColorSets = { PrimerColor.Rainbow.ToArray() };
    
    public bool ShowValuesOnBars = false;
    public float BarLabelScaleFactor = 1;
    public string BarLabelPrefix = "";
    public string BarLabelSuffix = "";
    public int BarLabelDecimalPlaces = 0;
    private Vector3 parentGraphSize => new Vector3(GetParent<Graph>().XAxis.length, GetParent<Graph>().YAxis.length, GetParent<Graph>().ZAxis.length);
    
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    
    public List<List<float>> Data;
    public delegate List<float>[] DataFetch();
    public DataFetch DataFetchMethod = () =>
    {
        PrimerGD.PrintWithStackTrace("Data fetch method not assigned. Returning empty list.");
        return Array.Empty<List<float>>();
    };

    public void FetchData()
    {
        Data = DataFetchMethod().Select(x => x.ToList()).ToList();
    }

    private List<List<Tuple<float, float, float>>> DataAsRectProperties()
    {
        var result = new List<List<Tuple<float, float, float>>>();
        
        // For each stack
        for (var stackIndex = 0; stackIndex < Data.Count; stackIndex++)
        {
            var stackSegments = new List<Tuple<float, float, float>>();
            
            // For each segment in the stack
            for (var segmentIndex = 0; segmentIndex < Data[stackIndex].Count; segmentIndex++)
            {
                var value = Data[stackIndex][segmentIndex];
                stackSegments.Add(
                    new Tuple<float, float, float>(
                        TransformPointFromDataSpaceToPositionSpace(new Vector3(stackIndex + _offset, 0, 0)).X,
                        TransformPointFromDataSpaceToPositionSpace(new Vector3(0, value, 0)).Y,
                        TransformPointFromDataSpaceToPositionSpace(new Vector3(_barWidth, 0, 0)).X
                    )
                );
            }
            result.Add(stackSegments);
        }
        return result;
    }
    
    private float _offset = 1;
    private float _barWidth = 0.8f;
    private float _barDepth = 0.01f;

    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        var stackProperties = DataAsRectProperties();
        
        if (!stackProperties.Any()) return null;
        
        var tween = CreateTween();
        tween.SetParallel();

        // For each stack position
        for (var stackIndex = 0; stackIndex < stackProperties.Count; stackIndex++)
        {
            var cumulativeHeight = 0f;
            
            // For each segment in the stack
            for (var segmentIndex = 0; segmentIndex < stackProperties[stackIndex].Count; segmentIndex++)
            {
                var segment = stackProperties[stackIndex][segmentIndex];
                var bar = GetNodeOrNull<MeshInstance3D>($"Bar {stackIndex} {segmentIndex}");
                
                if (bar == null)
                {
                    bar = CreateBar(stackIndex, segmentIndex, segment);
                }

                var segmentHeight = segment.Item2;
                var targetPosition = new Vector3(
                    segment.Item1, 
                    cumulativeHeight + segmentHeight / 2,
                    0
                );
                
                tween.TweenProperty(bar, "position", targetPosition, duration);
                tween.TweenProperty(bar, "mesh:size:y", segmentHeight, duration);
                tween.TweenProperty(bar, "mesh:size:x", segment.Item3, duration);

                if (ShowValuesOnBars)
                {
                    var theLabel = GetNode<LatexNode>($"Label {stackIndex} {segmentIndex}");
                    
                    var targetLabelPos = new Vector3(
                        segment.Item1,
                        cumulativeHeight + segmentHeight / 2, // Center vertically in bar segment
                        0.01f
                    );
                    tween.TweenProperty(theLabel, "position", targetLabelPos, duration);
                
                    var targetLabelScale = BarLabelScaleFactor * segment.Item3 / 2 * Vector3.One;
                    tween.TweenProperty(theLabel, "scale", targetLabelScale, duration);

                    theLabel.numberSuffix = BarLabelSuffix;
                    theLabel.numberPrefix = BarLabelPrefix;
                    theLabel.DecimalPlacesToShow = BarLabelDecimalPlaces;
                    
                    tween.TweenProperty(theLabel, "NumericalExpression", 
                        Mathf.RoundToInt(Data[stackIndex][segmentIndex]), duration);
                }

                cumulativeHeight += segmentHeight;
            }
        }

        return tween;
    }

    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        var stackProperties = DataAsRectProperties();
        var animations = new List<Animation>();

        // For each stack position
        for (var stackIndex = 0; stackIndex < stackProperties.Count; stackIndex++)
        {
            var cumulativeHeight = 0f;
            
            // For each segment in the stack
            for (var segmentIndex = 0; segmentIndex < stackProperties[stackIndex].Count; segmentIndex++)
            {
                var segment = stackProperties[stackIndex][segmentIndex];
                var bar = GetNodeOrNull<MeshInstance3D>($"Bar {stackIndex} {segmentIndex}");
                
                if (bar == null)
                {
                    bar = CreateBar(stackIndex, segmentIndex, segment);
                }

                var segmentHeight = segment.Item2;
                var targetPosition = new Vector3(
                    segment.Item1, 
                    cumulativeHeight + segmentHeight / 2,
                    0
                );
                
                animations.Add(bar.AnimateValue(targetPosition, "position"));
                animations.Add(bar.AnimateValue(segmentHeight, "mesh:size:y"));
                animations.Add(bar.AnimateValue(segment.Item3, "mesh:size:x"));

                if (ShowValuesOnBars)
                {
                    var theLabel = GetNode<LatexNode>($"Label {stackIndex} {segmentIndex}");
                    
                    var targetLabelPos = new Vector3(
                        segment.Item1,
                        cumulativeHeight + segmentHeight * parentGraphSize.Y / 10,
                        0.01f
                    );
                    animations.Add(theLabel.MoveTo(targetLabelPos));
                
                    var targetLabelScale = BarLabelScaleFactor * segment.Item3 / 2 * Vector3.One;
                    animations.Add(theLabel.ScaleTo(targetLabelScale));

                    theLabel.numberSuffix = BarLabelSuffix;
                    theLabel.numberPrefix = BarLabelPrefix;
                    theLabel.DecimalPlacesToShow = BarLabelDecimalPlaces;
                    
                    animations.Add(theLabel.AnimateNumericalExpression(Mathf.RoundToInt(Data[stackIndex][segmentIndex])));
                }

                cumulativeHeight += segmentHeight;
            }
        }

        return animations.RunInParallel();
    }

    private MeshInstance3D CreateBar(int stackIndex, int segmentIndex, Tuple<float, float, float> segment)
    {
        var bar = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = TransformPointFromDataSpaceToPositionSpace(new Vector3(_barWidth, 0, _barDepth));
        bar.Position = new Vector3(segment.Item1, 0, 0);
        bar.Mesh = mesh;
        bar.Name = $"Bar {stackIndex} {segmentIndex}";
        AddChild(bar);
        bar.Owner = GetTree().EditedSceneRoot;
        
        var newMat = new StandardMaterial3D();
        var colorSet = ColorSets[stackIndex % ColorSets.Length];
        newMat.AlbedoColor = colorSet[segmentIndex % colorSet.Length];
        bar.Mesh.SurfaceSetMaterial(0, newMat);

        if (ShowValuesOnBars)
        {
            var label = new LatexNode();
            label.latex = 0.ToString();
            label.Name = $"Label {stackIndex} {segmentIndex}";
            label.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
            label.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;
            label.UpdateCharacters();
            AddChild(label);
            label.Owner = GetTree().EditedSceneRoot;
            label.Position = bar.Position + Vector3.Up * parentGraphSize.Y / 10;
            label.Scale = Vector3.Zero;
        }

        return bar;
    }
    
    public Animation Disappear()
    {
        throw new NotImplementedException();
    }
    
    public void SetData(params List<float>[] stacks)
    {
        Data = stacks.ToList();
    }

    public void AddStackedData(params float[][] newData)
    {
        Data ??= new List<List<float>>();
        Data.AddRange(newData.Select(stack => stack.ToList()));
    }
}
