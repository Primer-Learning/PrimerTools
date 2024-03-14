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
			child.Free();
		}
		
		var path = await latexToMesh.MeshFromExpression(latex, openBlender);
		var newNode = ResourceLoader.Load<PackedScene>(path).Instantiate<Node3D>();
		
		AddChild(newNode);
		// Uncomment for testing a LaTeX object in its own scene.
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
