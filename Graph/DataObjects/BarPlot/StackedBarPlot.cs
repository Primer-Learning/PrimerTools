using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.LaTeX;
using System.Text.Json;
using System.IO;

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
    
    // TODO: Probably just make Data a float[][]
    // Or maybe not, since adding to that constantly re-allocates.
    // Probably doesn't matter.
    public List<List<float>> Data;
    public int StackCountLimit = 0; // zero means no limit
    private List<List<float>> PlottedData
    {
        get
        {
            if (StackCountLimit <= 0) return Data;
            return Data.Take(StackCountLimit).ToList();
        }
    }

    public delegate float[][] DataFetch();
    public DataFetch DataFetchMethod = () =>
    {
        PrimerGD.PrintWithStackTrace("Data fetch method not assigned. Returning empty list.");
        return Array.Empty<float[]>();
    };

    public void FetchData()
    {
        Data = DataFetchMethod().Select(x => x.ToList()).ToList();
    }

    private List<List<Tuple<float, float, float>>> DataAsRectProperties()
    {
        var result = new List<List<Tuple<float, float, float>>>();
        
        // For each stack
        for (var stackIndex = 0; stackIndex < PlottedData.Count; stackIndex++)
        {
            var stackSegments = new List<Tuple<float, float, float>>();
            
            // For each segment in the stack
            for (var segmentIndex = 0; segmentIndex < Data[stackIndex].Count; segmentIndex++)
            {
                var value = Data[stackIndex][segmentIndex];
                stackSegments.Add(
                    new Tuple<float, float, float>(
                        TransformPointFromDataSpaceToPositionSpace(new Vector3(stackIndex + Offset, 0, 0)).X,
                        TransformPointFromDataSpaceToPositionSpace(new Vector3(0, value, 0)).Y,
                        TransformPointFromDataSpaceToPositionSpace(new Vector3(_barWidth, 0, 0)).X
                    )
                );
            }
            result.Add(stackSegments);
        }
        return result;
    }
    
    // TODO: Make this follow the pattern in BarPlot where there is a bar width and a fill factor.
    // This allows for bars to span more than one unit, such as a histogram grouping things by fives.
    public float Offset = 1;
    private float _barWidth = 0.8f;
    private float _barDepth = 0.01f;

    public IStateChange TransitionStateChange(double duration)
    {
        throw new NotImplementedException();
    }

    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        var stackProperties = DataAsRectProperties();
        
        if (!stackProperties.Any() && GetChildren().Count == 0) return null;

        // We track bars in the scene, since stored values get lost when rebuilding the project.
        var remainingBars = GetChildren().OfType<MeshInstance3D>().ToList(); 
        
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
                remainingBars.Remove(bar);
                
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

        // Shrink remaining bars
        foreach (var bar in remainingBars)
        {
            tween.TweenProperty(bar, "position:y", 0, duration);
            tween.TweenProperty(bar, "mesh:size:y", 0, duration);
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
                    animations.Add(theLabel.MoveToAnimation(targetLabelPos));
                
                    var targetLabelScale = BarLabelScaleFactor * segment.Item3 / 2 * Vector3.One;
                    animations.Add(theLabel.ScaleToAnimation(targetLabelScale));

                    theLabel.numberSuffix = BarLabelSuffix;
                    theLabel.numberPrefix = BarLabelPrefix;
                    theLabel.DecimalPlacesToShow = BarLabelDecimalPlaces;
                    
                    animations.Add(theLabel.AnimateNumericalExpression(Mathf.RoundToInt(Data[stackIndex][segmentIndex])));
                }

                cumulativeHeight += segmentHeight;
            }
        }

        return animations.InParallel();
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
            label.Latex = 0.ToString();
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

    public void SaveData(string filePath, bool globalPath = false)
    {
        string finalPath = globalPath ? filePath : ProjectSettings.GlobalizePath(filePath);
        if (!finalPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            finalPath += ".json";
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
        var jsonString = JsonSerializer.Serialize(Data.Select(list => list.ToArray()).ToArray());
        File.WriteAllText(finalPath, jsonString);
    }

    public void LoadData(string filePath, bool globalPath = false)
    {
        string finalPath = globalPath ? filePath : ProjectSettings.GlobalizePath(filePath);
        if (!finalPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File path must end with .json extension");
        if (!File.Exists(finalPath))
            throw new FileNotFoundException($"Data file not found at {finalPath}");
            
        var jsonString = File.ReadAllText(finalPath);
        var loadedData = JsonSerializer.Deserialize<float[][]>(jsonString);
        Data = loadedData.Select(array => array.ToList()).ToList();
    }
}
