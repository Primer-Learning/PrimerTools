using System.Linq;
using Godot;

namespace PrimerTools.LaTeX;
[Tool]
public partial class LatexNode : Node3D
{
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
	[Export] public string latex = "$z^2 + y^2 = 1$";
	
	private readonly LatexToMesh latexToMesh = new();
	
	public async void UpdateCharacters() {
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		
		var path = await latexToMesh.MeshFromExpression(latex, openBlender);
		var newNode = ResourceLoader.Load<PackedScene>(path).Instantiate<Node3D>();
		
		AddChild(newNode);
		newNode.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
		newNode.RotationDegrees = new Vector3(0, 0, 0);
		
		Align();
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
	
	private VerticalAlignmentOptions verticalAlignment = VerticalAlignmentOptions.Baseline;
	private HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left;
	
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
		GD.Print($"Top: {top}");
		GD.Print($"Bottom: {bottom}");
		GD.Print($"Left: {left}");
		GD.Print($"Right: {right}");
		
		float x, y;
		switch (horizontalAlignment)
		{
			case HorizontalAlignmentOptions.Left:
				x = -left;
				break;
			case HorizontalAlignmentOptions.Right:
				x = -right;
				break;
			case HorizontalAlignmentOptions.Center:
				x = -(left + right) / 2;
				break;
			default:
				x = 0;
				break;
		}

		switch (verticalAlignment)
		{
			case VerticalAlignmentOptions.Bottom:
				y = -bottom;
				break;
			case VerticalAlignmentOptions.Top:
				y = -top;
				break;
			case VerticalAlignmentOptions.Center:
				y = -(bottom + top) / 2;
				break;
			case VerticalAlignmentOptions.Baseline:
				y = 0;
				break;
			default:
				y = 0;
				break;
		}
		
		((Node3D)GetChild(0)).Position = new Vector3(x, y,0);
	}

	#endregion
}
