using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using PrimerTools.Graph;

namespace PrimerTools.LaTeX;
[Tool]
public partial class LatexNode : Node3D
{
	public static LatexNode Create(string latex)
	{
		var node = new LatexNode();
		node.Latex = latex;
		if (latex != "")
		{
			node.UpdateCharacters();
			node.Name = latex;
		}
		return node;
	}

	public LatexNode(string latex)
	{
		Latex = latex;
		if (latex != "")
		{
			UpdateCharacters();
			Name = latex;
		}
	}
	
	public LatexNode() {}
	
	public List<MeshInstance3D> GetCharacters()
	{
		if (GetChildren().Count == 0 || GetChild(0).GetChildren().Count == 0)
		{
			GD.PushWarning("LatexNode has no valid characters");
			return new List<MeshInstance3D>();
		}

		return GetChild(0).GetChildren().OfType<MeshInstance3D>().ToList();
	}


	private bool run = true;
	[Export] public bool Run {
		get => run;
		set {
			var oldRun = run;
			run = value;
			if (run && !oldRun) { // Avoids running on build
				UpdateCharacters();
			}
		}
	}

	[Export] public bool openBlender = false;
	[Export] public string Latex = "\\LaTeX";

	public string numberPrefix = "";
	public string numberSuffix = "";
	public int DecimalPlacesToShow = 0;
	public bool showApproxAtZero = false;
	private float number;
	public float NumericalExpression {
		get => number;
		set
		{
			var precision = Mathf.Pow(10, DecimalPlacesToShow);
			var rounded = Mathf.Round(value * precision) / precision;
			var approx = "";
			// Whether to show the approximation symbol
			// For now, we'll just show it if the number is zero
			// if (Mathf.Abs(value - rounded) > 0.00001)
			if (rounded == 0 && value != 0 && showApproxAtZero)
			{
				approx = "\\sim ";
			}
			Latex = "$" + approx + numberPrefix + value.ToString("F" + DecimalPlacesToShow) + numberSuffix + "$";
			UpdateCharacters();
			
			number = value;
		}
	}

	public Animation AnimateNumericalExpression(float value, double duration = AnimationUtilities.DefaultDuration)
	{
		var labelTextAnimation = new Animation();
		var trackCount = labelTextAnimation.AddTrack(Animation.TrackType.Value);
		labelTextAnimation.TrackSetPath(trackCount, GetPath() + ":NumericalExpression");
		labelTextAnimation.TrackInsertKey(trackCount, 0, NumericalExpression);
		labelTextAnimation.TrackInsertKey(trackCount, duration, value);
		labelTextAnimation.TrackSetInterpolationType(trackCount, Animation.InterpolationType.Linear);
		NumericalExpression = value;

		labelTextAnimation.Length = (float)duration;
		return labelTextAnimation;
	}
	
	private readonly LatexToMesh latexToMesh = new();

	private List<MeshInstance3D> Characters;
	public async void UpdateCharacters() {
		if (Latex == "") return;
		foreach (var child in GetChildren())
		{
			child.Free();
		}

		var path = await latexToMesh.MeshFromExpression(Latex, openBlender);

		// Runtime GLTF loading
		Node3D newNode;
		if (!Engine.IsEditorHint())
		{
			// At runtime, use direct GLTF loading
			var gltfDocument = new GltfDocument();
			var gltfState = new GltfState();

			// Load the GLTF file
			var error = gltfDocument.AppendFromFile(path, gltfState);
			if (error != Error.Ok)
			{
				GD.PushError($"Failed to load GLTF file at runtime: {path}, Error: {error}");
				newNode = new Node3D();
			}
			else
			{
				// Generate the scene from the loaded GLTF
				// If this is the first time we've generated the expression, this still fails to produce characters
				// But it doesn't throw errors and break the scene, so that's something.
				newNode = (Node3D)gltfDocument.GenerateScene(gltfState);
				if (newNode == null)
				{
					GD.PushError($"Failed to generate scene from GLTF: {path}");
					newNode = new Node3D();
				}
			}
		}
		else
		{
			// In editor, use the import system
			EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(path);
			EditorInterface.Singleton.GetResourceFilesystem().ReimportFiles(new string[] {path});

			try
			{
				newNode = ResourceLoader.Load<PackedScene>(path).Instantiate<Node3D>();
			}
			catch
			{
				GD.PushWarning($"Loading LaTeX object failed in editor: {path}");
				newNode = new Node3D();
			}
		}

		AddChild(newNode);
		newNode.RotationDegrees = new Vector3(0, 0, 0);

		Align();
	}

	// TODO: Consider using GetOrCreateOverrideMaterial instead of MakeMaterialUnique.
	// Should be essentially the same thing,
	// but it could be used in the same loop rather than needing a separate flag and method.
	// It's slightly more checks, but pretty negligible.
	public void SetColor(Color color, int beginIndex = 0, int endIndex = -1)
	{
		MakeMaterialUnique();

		var characters = GetCharacters();
		if (endIndex == -1)
		{
			endIndex = characters.Count;
		}
		foreach (var character in characters.Take(new System.Range(beginIndex, endIndex)))
		{
			((StandardMaterial3D)((MeshInstance3D)character).Mesh.SurfaceGetMaterial(0)).AlbedoColor = color;
		}
	}

	private bool _materialsAreUnique = false;
	private void MakeMaterialUnique()
	{
		if (_materialsAreUnique) return;
		foreach (var child in GetChildren())
		{
			foreach (var grandchild in child.GetChildren())
			{
				if (grandchild is MeshInstance3D meshInstance3D)
				{
					// Old duplication that kept the material from blender import.
					// meshInstance3D.Mesh.SurfaceSetMaterial(0,(StandardMaterial3D) meshInstance3D.Mesh.SurfaceGetMaterial(0).Duplicate(true));
					// Instead, create a new material with basic defaults.
					meshInstance3D.Mesh.SurfaceSetMaterial(0, new StandardMaterial3D());
				}
			}
		}

		_materialsAreUnique = true;
	}
	public Animation AnimateColor(Color color)
	{
		MakeMaterialUnique();
		var animations = new List<Animation>();
		foreach (var child in GetChildren())
		{
			foreach (var grandchild in child.GetChildren())
			{
				if (grandchild is MeshInstance3D meshInstance3D)
				{
					animations.Add(
						meshInstance3D.AnimateColorRgb(color)
					);
				}
			}
		}
		return animations.InParallel();
	}
	
	#region Alignment

	public enum HorizontalAlignmentOptions
	{
		Left,
		Center,
		Right
	}
	public enum VerticalAlignmentOptions
	{
		Top,
		Center,
		Baseline,
		Bottom
	}
	
	private VerticalAlignmentOptions verticalAlignment = VerticalAlignmentOptions.Center;
	private HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Center;
	
	[Export] public VerticalAlignmentOptions VerticalAlignment {
		get => verticalAlignment;
		set {
			verticalAlignment = value;
			Align();
		}
	}
	[Export] public HorizontalAlignmentOptions HorizontalAlignment {
		get => horizontalAlignment;
		set {
			horizontalAlignment = value;
			Align();
		}
	}

	private void Align()
	{
		if (GetChildCount() == 0)
		{
			// GD.PushWarning("No children to align.");
			return;
		}
		var children = GetChild(0).GetChildren().OfType<VisualInstance3D>();
		var visualInstance3Ds = children as VisualInstance3D[] ?? children.ToArray();
		
		var left = visualInstance3Ds.Select(x => x.GetAabb().Position.X * x.Scale.X + x.Position.X).Min();
		var right = visualInstance3Ds.Select(x => x.GetAabb().End.X * x.Scale.X + x.Position.X).Max();
		var bottom = visualInstance3Ds.Select(x => x.GetAabb().Position.Y * x.Scale.Y + x.Position.Y).Min();
		var top = visualInstance3Ds.Select(x => x.GetAabb().End.Y * x.Scale.Y + x.Position.Y).Max();
		
		float x, y;
		x = horizontalAlignment switch
		{
			HorizontalAlignmentOptions.Left => -left,
			HorizontalAlignmentOptions.Right => -right,
			HorizontalAlignmentOptions.Center => -(left + right) / 2,
			_ => 0
		};

		y = verticalAlignment switch
		{
			VerticalAlignmentOptions.Bottom => -bottom,
			VerticalAlignmentOptions.Top => -top,
			VerticalAlignmentOptions.Center => -(bottom + top) / 2,
			VerticalAlignmentOptions.Baseline => 0,
			_ => 0
		};

		((Node3D)GetChild(0)).Position = new Vector3(x, y,0);
	}

	#endregion
}
