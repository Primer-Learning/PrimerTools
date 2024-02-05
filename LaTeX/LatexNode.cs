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
		var newNode = ResourceLoader.Load<PackedScene>(path).Instantiate();
		
		AddChild(newNode);
		// newNode.Owner = this;
		// newNode.Owner = GetTree().EditedSceneRoot;
		
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
		var top = visualInstance3Ds.Select(x => x.GetAabb().Position.Z * x.Scale.Z + x.Position.Z).Min();
		var bottom = visualInstance3Ds.Select(x => x.GetAabb().End.Z * x.Scale.Z + x.Position.Z).Max();

		float x, z;
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
				z = -bottom;
				break;
			case VerticalAlignmentOptions.Top:
				z = -top;
				break;
			case VerticalAlignmentOptions.Center:
				z = -(bottom + top) / 2;
				break;
			case VerticalAlignmentOptions.Baseline:
				z = 0;
				break;
			default:
				z = 0;
				break;
		}
		
		((Node3D)GetChild(0)).Position = new Vector3(x,0, z);
	}

	#endregion
}
