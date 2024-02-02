using Godot;
using System.Linq;

[Tool]
public partial class AABBPrinter : VisualInstance3D
{
	private bool printBounds = true;
	[Export] public bool PrintBounds {
		get => printBounds;
		set {
			var oldPrintBounds = printBounds;
			printBounds = value;
			if (printBounds && !oldPrintBounds) { // Avoids running on build
				PrintBoundingBox();
			}
		}
	}
	
	public override void _Ready()
	{
		PrintBoundingBox();
	}

	private void PrintBoundingBox()
	{
		GD.Print("Bounds: " + GetAabb().Position, GetAabb().End);
	}
}
