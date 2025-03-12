using Godot;
using PrimerTools.Simulation;

public partial class FruitGrowthSlider : HSlider
{
	public override void _Ready()
	{
		base._Ready();
		Value = FruitTreeSimSettings.FruitGrowthTime;
	}

	public override void _ValueChanged(double newValue)
	{
		FruitTreeSimSettings.FruitGrowthTime = (float) newValue;
	}
}
