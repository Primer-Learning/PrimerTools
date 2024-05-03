using Godot;
using PrimerAssets;
using PrimerTools;
using PrimerTools.AnimationSequence;
using PrimerTools.Graph;

[Tool]
public partial class TernaryGraphTestSequence : AnimationSequence
{
	protected override void Define()
	{
		var ternaryPlot = new TernaryGraphWithBars();
		ternaryPlot.Colors = new[]
		{
			PrimerColor.red,
			PrimerColor.blue,
			PrimerColor.yellow
		};
		AddChild(ternaryPlot);
		ternaryPlot.CreateBounds();

		RegisterAnimation(ternaryPlot.ScaleTo(1));

		ternaryPlot.BarsPerSide = 11;
		
		var numBars = ternaryPlot.BarsPerSide * (ternaryPlot.BarsPerSide + 1) / 2;
		var data = new float[numBars];
		var rng = new System.Random(System.Environment.TickCount);
		for (var i = 0; i < numBars; i++)
		{
			data[i] = (float)rng.NextDouble();
		}

		ternaryPlot.Data = data;
		
		ternaryPlot.AddBars();
		ternaryPlot.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
	}
}
