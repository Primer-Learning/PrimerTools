using Godot;
using PrimerTools.Latex;

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
			if (run && !oldRun) { // Avoids running on build
				Process();
			}
		}
	}
	
	[Export] public string _latex = "$x^2 + y^2 = 1$";
	
	private readonly LatexToSvg latexToSvg = new();

	private async void Process()
	{
		GD.Print("Running the test");
		var input = LatexInput.From(_latex);
		var svg = await latexToSvg.RenderToSvg(input, default);
		GD.Print(svg);
	}
}
