using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerAssets;
using PrimerTools.LaTeX;
using System.Text.Json;
using System.IO;

namespace PrimerTools.Graph;

public partial class BarPlot : Node3D, IPrimerGraphData
{
    public Color[] Colors = PrimerColor.Rainbow.ToArray();
    
    public bool ShowValuesOnBars = false;
    public float BarLabelScaleFactor = 1;
    public float BarLabelVerticalOffset = 0.5f;
    public string BarLabelPrefix = "";
    public string BarLabelSuffix = "";
    public int BarLabelDecimalPlaces = 0;
    private Vector3 parentGraphSize => new Vector3(GetParent<Graph>().XAxis.length, GetParent<Graph>().YAxis.length, GetParent<Graph>().ZAxis.length);
    
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    
    private Graph ParentGraph => GetParent<Graph>();
    
    private Vector3 GetAppropriateTransformation(Vector3 point, bool useTween)
    {
        if (useTween)
            return ParentGraph.GetDataSpaceToPositionSpaceFromCurrentObjects(point);
        return TransformPointFromDataSpaceToPositionSpace(point);
    }

    private List<float> _data;
    public IReadOnlyList<float> Data => _data.AsReadOnly();
    public int BarCountLimit = 0; // zero means no limit
    private List<float> PlottedData
    {
        get
        {
            if (BarCountLimit <= 0) return _data;
            return _data.Take(BarCountLimit).ToList();
        }
    }
    
    public delegate float[] DataFetch();
    public DataFetch DataFetchMethod = () =>
    {
        PrimerGD.PrintWithStackTrace("Data fetch method not assigned. Returning empty list.");
        return Array.Empty<float>();
    };
    public void FetchData()
    {
        _data = DataFetchMethod().ToList();
    }

    private List<Tuple<float, float, float>> DataAsRectProperties(bool useTween = false)
    {
        return PlottedData.Select( (value, i) =>
            new Tuple<float, float, float>(
                GetAppropriateTransformation(new Vector3((i + OffsetInBarWidthUnits) * BarWidth, 0, 0), useTween).X,
                GetAppropriateTransformation(new Vector3(0, value, 0), useTween).Y,
                GetAppropriateTransformation(new Vector3(_barWidthFillFactor * BarWidth, 0, 0), useTween).X
            )
        ).ToList();
    }

    public float BarWidth = 1;
    public float OffsetInBarWidthUnits = 1;
    private float _barWidthFillFactor = 0.8f;
    private float _barDepth = 0.01f;

    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        var rectProperties = DataAsRectProperties(useTween: false);
        var animations = new List<Animation>();
        
        // Keep track of updated bars
        var updatedBars = new HashSet<string>();

        // Iterate through the data points
        for (var i = 0; i < rectProperties.Count; i++)
        {
            var barName = $"Bar {i}";
            updatedBars.Add(barName);
            
            var bar = GetNodeOrNull<MeshInstance3D>(barName);
            // If the bar doesn't exist, make it
            if (bar == null)
            {
                bar = CreateBar(i, rectProperties[i]);
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

                theLabel.numberSuffix = BarLabelSuffix;
                theLabel.numberPrefix = BarLabelPrefix;
                theLabel.DecimalPlacesToShow = BarLabelDecimalPlaces;
                
                animations.Add(theLabel.AnimateNumericalExpression(Mathf.RoundToInt(_data[i])));
            }
        }

        // Handle any remaining bars that aren't in the current data set
        foreach (var child in GetChildren())
        {
            if (child is MeshInstance3D bar && !updatedBars.Contains(child.Name))
            {
                // Animate the bar to zero height
                animations.Add(bar.AnimateValue(0f, "mesh:size:y"));
                
                // Move it to y=0 position
                animations.Add(bar.AnimateValue(new Vector3(bar.Position.X, 0, bar.Position.Z), "position"));

                // Handle associated label if it exists
                var labelName = $"Label {child.Name.ToString().Split(' ')[1]}";
                if (GetNodeOrNull<LatexNode>(labelName) is { } label)
                {
                    animations.Add(label.ScaleTo(Vector3.Zero));
                }
            }
        }

        return animations.InParallel();
    }

    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        var rectProperties = DataAsRectProperties(useTween: true);

        if (!rectProperties.Any() && GetChildren().Count == 0) return null;

        var tween = CreateTween();
        tween.SetParallel();
        
        // Keep track of updated bars
        var updatedBars = new HashSet<string>();
        
        // Iterate through the data points
        for (var i = 0; i < rectProperties.Count; i++)
        {
            var barName = $"Bar {i}";
            updatedBars.Add(barName);
            
            var bar = GetNodeOrNull<MeshInstance3D>(barName);
            // If the bar doesn't exist, make it
            if (bar == null)
            {
                bar = CreateBar(i, rectProperties[i]);
            }
            
            // Position animation
            var targetPosition = new Vector3(rectProperties[i].Item1, rectProperties[i].Item2 / 2, 0);
            tween.TweenProperty(
                bar,
                "position",
                targetPosition,
                duration
            );
            
            // Height animation
            var targetHeight = rectProperties[i].Item2;
            tween.TweenProperty(
                bar,
                "mesh:size:y",
                targetHeight,
                duration
            );
            
            // Width animation
            var targetWidth = rectProperties[i].Item3;
            tween.TweenProperty(
                bar,
                "mesh:size:x",
                targetWidth,
                duration
            );
            
            if (ShowValuesOnBars)
            {
                var theLabel = GetNode<LatexNode>($"Label {i}");
                
                var targetLabelPos = new Vector3(rectProperties[i].Item1,
                    rectProperties[i].Item2 + BarLabelVerticalOffset * parentGraphSize.Y / 10, 0);
                tween.TweenProperty(
                    theLabel,
                    "position",
                    targetLabelPos,
                    duration
                );
                
                var targetLabelScale = BarLabelScaleFactor * rectProperties[i].Item3 / 2 * Vector3.One;
                tween.TweenProperty(
                    theLabel,
                    "scale",
                    targetLabelScale,
                    duration
                );

                theLabel.numberSuffix = BarLabelSuffix;
                theLabel.numberPrefix = BarLabelPrefix;
                theLabel.DecimalPlacesToShow = BarLabelDecimalPlaces;
                
                tween.TweenProperty(
                    theLabel,
                    "NumericalExpression",
                    Mathf.RoundToInt(_data[i]),
                    duration
                );
            }
        }
        
        // Handle any remaining bars that aren't in the current data set
        foreach (var child in GetChildren())
        {
            if (child is MeshInstance3D bar && !updatedBars.Contains(child.Name))
            {
                // Animate the bar to zero height
                tween.TweenProperty(
                    bar,
                    "mesh:size:y",
                    0f,
                    duration
                );
                
                // Move it to y=0 position
                tween.TweenProperty(
                    bar,
                    "position",
                    new Vector3(bar.Position.X, 0, bar.Position.Z),
                    duration
                );

                // Handle associated label if it exists
                var labelName = $"Label {child.Name.ToString().Split(' ')[1]}";
                if (GetNodeOrNull<LatexNode>(labelName) is { } label)
                {
                    tween.TweenProperty(
                        label,
                        "scale",
                        Vector3.Zero,
                        duration
                    );
                }
            }
        }

        return tween;
    }

    private MeshInstance3D CreateBar(int i, Tuple<float, float, float> rectProperties)
    {
        var bar = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = TransformPointFromDataSpaceToPositionSpace(new Vector3(_barWidthFillFactor * BarWidth, 0, _barDepth));
        bar.Position = new Vector3(rectProperties.Item1, 0, 0);
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

        return bar;
    }
    
    public Animation Disappear()
    {
        throw new System.NotImplementedException();
    }
    
    public void SetData(params float[] bars)
    {
        _data = bars.ToList();
    }

    public void AddData(params float[] newData)
    {
        _data ??= new List<float>();
        _data.AddRange(newData);
    }

    public void SaveData(string filePath, bool globalPath = false)
    {
        string finalPath = globalPath ? filePath : ProjectSettings.GlobalizePath(filePath);
        if (!finalPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            finalPath += ".json";
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
        var jsonString = JsonSerializer.Serialize(_data.ToArray());
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
        var loadedData = JsonSerializer.Deserialize<float[]>(jsonString);
        _data = loadedData.ToList();
    }
}
