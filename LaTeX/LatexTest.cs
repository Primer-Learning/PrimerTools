using Godot;

namespace PrimerTools.LaTeX;
[Tool]
public partial class LatexTest : Node3D
{
	private bool run = true;
	[Export]
	public bool Run {
		get => run;
		set {
			var oldRun = run;
			run = value;
			if (run && !oldRun) {
				GD.Print("Running the test");
			}
		}
	}
	
	// [Export] public string _latex = "x^2 + y^2 = 1";
	
	private readonly LatexToSvg latexToSvg = new();

	private void Process()
	{
		
	}
}
