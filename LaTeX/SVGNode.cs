using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

namespace PrimerTools.LaTeX;

[Tool]
public partial class SVGNode : Node3D
{
	private readonly SvgToMesh svgToMesh = new();
	
	public static SVGNode Create(string svgFilePath)
	{
		var node = new SVGNode();
		node.SvgFilePath = svgFilePath;
		if (!string.IsNullOrEmpty(svgFilePath))
		{
			node.UpdateMesh();
			node.Name = Path.GetFileNameWithoutExtension(svgFilePath);
		}
		return node;
	}
	
	public SVGNode(string svgFilePath)
	{
		SvgFilePath = svgFilePath;
		if (!string.IsNullOrEmpty(svgFilePath))
		{
			UpdateMesh();
			Name = Path.GetFileNameWithoutExtension(svgFilePath);
		}
	}
	
	public SVGNode() {}
	
	public List<MeshInstance3D> GetCharacters()
	{
		if (GetChildren().Count == 0 || GetChild(0).GetChildren().Count == 0)
		{
			GD.PushWarning("SVGNode has no valid characters");
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
				UpdateMesh();
			}
		}
	}
	
	[Export] public bool openBlender = false;
	[Export(PropertyHint.File, "*.svg")] public string SvgFilePath = "";
	
	public bool Billboard = false;
	
	public override void _Process(double delta)
	{
		if (Billboard)
		{
			var scale = Scale;
			
			var forward = (GetViewport().GetCamera3D().GlobalPosition - GlobalPosition).Normalized();
			var basis = Transform3DUtils.BasisFromForwardAndUp(forward, Vector3.Up);
			GlobalBasis = basis;
			Scale = scale;
		}
	}
	
	private string ResolveFilePath(string path)
	{
		// If it's a Godot resource path, convert to OS path
		if (path.StartsWith("res://"))
		{
			return ProjectSettings.GlobalizePath(path);
		}
		// If it's already an absolute path, use it as-is
		return path;
	}
	
	public async void UpdateMesh()
	{
		if (string.IsNullOrEmpty(SvgFilePath)) return;
		
		// Resolve the path to an OS path for file operations
		var resolvedPath = ResolveFilePath(SvgFilePath);
		
		// Check if the SVG file exists
		if (!File.Exists(resolvedPath))
		{
			GD.PushError($"SVG file not found: {SvgFilePath} (resolved to: {resolvedPath})");
			return;
		}
		
		// Clear existing children
		foreach (var child in GetChildren())
		{
			child.Free();
		}
		
		// Use filename (without extension) plus file hash as identifier for caching
		var fileContent = File.ReadAllText(resolvedPath);
		var identifier = Path.GetFileNameWithoutExtension(SvgFilePath) + "_" + fileContent.GetDeterministicHashCode();
		
		var meshPath = svgToMesh.GetPathToExisting(identifier);
		GD.Print($"Mesh path: {meshPath}");
		if (string.IsNullOrEmpty(meshPath))
		{
			GD.Print("Making mesh");
			meshPath = await svgToMesh.ConvertSvgToMesh(resolvedPath, identifier, openBlender);
		}
		
		// Runtime GLTF loading
		Node3D newNode;
		if (!Engine.IsEditorHint())
		{
			// At runtime, use direct GLTF loading
			var gltfDocument = new GltfDocument();
			var gltfState = new GltfState();

			// Load the GLTF file
			var error = gltfDocument.AppendFromFile(meshPath, gltfState);
			if (error != Error.Ok)
			{
				GD.PushError($"Failed to load GLTF file at runtime: {meshPath}, Error: {error}");
				newNode = new Node3D();
			}
			else
			{
				// Generate the scene from the loaded GLTF
				newNode = (Node3D)gltfDocument.GenerateScene(gltfState);
				if (newNode == null)
				{
					GD.PushError($"Failed to generate scene from GLTF: {meshPath}");
					newNode = new Node3D();
				}
			}
		}
		else
		{
			// In editor, use the import system
			EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(meshPath);
			EditorInterface.Singleton.GetResourceFilesystem().ReimportFiles(new string[] {meshPath});

			try
			{
				newNode = ResourceLoader.Load<PackedScene>(meshPath).Instantiate<Node3D>();
			}
			catch
			{
				GD.PushWarning($"Loading SVG mesh failed in editor: {meshPath}");
				newNode = new Node3D();
			}
		}

		AddChild(newNode);
		newNode.RotationDegrees = new Vector3(0, 0, 0);

		Align();
	}
	
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
			return;
		}
		var children = GetChild(0).GetChildren().OfType<VisualInstance3D>();
		var visualInstance3Ds = children as VisualInstance3D[] ?? children.ToArray();
		
		if (!visualInstance3Ds.Any())
		{
			return;
		}
		
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

		((Node3D)GetChild(0)).Position = new Vector3(x, y, 0);
	}

	#endregion
}
