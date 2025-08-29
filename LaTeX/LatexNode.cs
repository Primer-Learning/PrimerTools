using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using PrimerTools.Graph;
using PrimerTools.TweenSystem;

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

		// Now we need to go one level deeper to get the MeshInstance3D children
		// since each character is wrapped in a Node3D container
		var characters = new List<MeshInstance3D>();
		foreach (var container in GetChild(0).GetChildren())
		{
			if (container is Node3D node3d)
			{
				foreach (var child in node3d.GetChildren())
				{
					if (child is MeshInstance3D meshInstance)
					{
						characters.Add(meshInstance);
						break; // Only expect one MeshInstance3D per container
					}
				}
			}
		}
		return characters;
	}

	public List<Node3D> GetCharacterContainers()
	{
		if (GetChildren().Count == 0 || GetChild(0).GetChildren().Count == 0)
		{
			GD.PushWarning("LatexNode has no valid character containers");
			return new List<Node3D>();
		}

		return GetChild(0).GetChildren().OfType<Node3D>().ToList();
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
	[Export] public float MidlineOffset = 0.35f; // Configurable offset from baseline to midline
	[Export] public bool UseMidlineCentering = true; // Make midline centering optional

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

	public bool Billboard = false;

	public override void _Process(double delta)
	{
		if (Billboard)
		{
			// GD.Print("arstoienarst");
			// LookAt(GetParent<Node3D>().ToLocal(GetViewport().GetCamera3D().GlobalPosition), Vector3.Up, useModelFront: true);
			// LookAt(ToLocal(GetViewport().GetCamera3D().GlobalPosition), Vector3.Up, useModelFront: true);

			var scale = Scale;
			
			var forward = (GetViewport().GetCamera3D().GlobalPosition - GlobalPosition).Normalized();
			var basis = Transform3DUtils.BasisFromForwardAndUp(forward, Vector3.Up);
			GlobalBasis = basis;
			Scale = scale;
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

		var path = latexToMesh.GetPathToExisting(Latex);
		if (path == string.Empty) path = await latexToMesh.MeshFromExpression(Latex, openBlender);

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

		if (UseMidlineCentering)
		{
			foreach (var container in newNode.GetChildren().OfType<Node3D>())
			{
				var diff = container.Position.Y - MidlineOffset;
				container.Position -= Vector3.Up * diff;
				container.GetChild<Node3D>(0).Position += Vector3.Up * diff;
			}
		}

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
				// Now we need to go one level deeper due to the container nodes
				if (grandchild is Node3D container)
				{
					foreach (var greatGrandchild in container.GetChildren())
					{
						if (greatGrandchild is MeshInstance3D meshInstance3D)
						{
							// Old duplication that kept the material from blender import.
							// meshInstance3D.Mesh.SurfaceSetMaterial(0,(StandardMaterial3D) meshInstance3D.Mesh.SurfaceGetMaterial(0).Duplicate(true));
							// Instead, create a new material with basic defaults.
							meshInstance3D.Mesh.SurfaceSetMaterial(0, new StandardMaterial3D());
						}
					}
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
				// Now we need to go one level deeper due to the container nodes
				if (grandchild is Node3D container)
				{
					foreach (var greatGrandchild in container.GetChildren())
					{
						if (greatGrandchild is MeshInstance3D meshInstance3D)
						{
							animations.Add(
								meshInstance3D.AnimateColorRgb(color)
							);
						}
					}
				}
			}
		}
		return animations.InParallel();
	}

	public CompositeStateChange Appear()
	{
		var containers = GetCharacterContainers();
		var stateChanges = new IStateChange[containers.Count];

		// Since midline centering is now done in UpdateCharacters,
		// we just need to scale the containers from zero
		var i = 0;
		foreach (var container in containers)
		{
			container.Scale = Vector3.Zero;
			stateChanges[i] = container.ScaleTo(1);
			i++;
		}
		return CompositeStateChange.Parallel(stateChanges);
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
		
		// Get all the visual instances (now they're the MeshInstance3D children within containers)
		var visualInstance3Ds = new List<VisualInstance3D>();
		foreach (var container in GetChild(0).GetChildren())
		{
			if (container is Node3D node3d)
			{
				foreach (var child in node3d.GetChildren())
				{
					if (child is VisualInstance3D visualInstance)
					{
						visualInstance3Ds.Add(visualInstance);
					}
				}
			}
		}
		
		if (visualInstance3Ds.Count == 0) return;
		
		// Calculate bounds considering the container transforms
		var boundsData = new List<(float left, float right, float bottom, float top)>();
		for (int i = 0; i < visualInstance3Ds.Count; i++)
		{
			var visualInstance = visualInstance3Ds[i];
			var container = visualInstance.GetParent<Node3D>();
			var aabb = visualInstance.GetAabb();
			
			// Transform the AABB corners by both the mesh's local transform and the container's transform
			var meshTransform = visualInstance.Transform;
			var containerTransform = container.Transform;
			var combinedTransform = containerTransform * meshTransform;
			
			var localMin = aabb.Position;
			var localMax = aabb.End;
			
			// Transform the min and max points
			var transformedMin = combinedTransform * localMin;
			var transformedMax = combinedTransform * localMax;
			
			boundsData.Add((
				Math.Min(transformedMin.X, transformedMax.X),
				Math.Max(transformedMin.X, transformedMax.X),
				Math.Min(transformedMin.Y, transformedMax.Y),
				Math.Max(transformedMin.Y, transformedMax.Y)
			));
		}
		
		var left = boundsData.Select(b => b.left).Min();
		var right = boundsData.Select(b => b.right).Max();
		var bottom = boundsData.Select(b => b.bottom).Min();
		var top = boundsData.Select(b => b.top).Max();
		
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

		GetChild<Node3D>(0).Position = new Vector3(x, y, 0);
	}

	#endregion
}
