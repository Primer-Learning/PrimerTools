using Godot;
using PrimerTools.Simulation;

public partial class FruitGrowthSlider : HSlider
{
	public override void _Ready()
	{
		base._Ready();
		Value = FruitTreeBehaviorHandler.FruitGrowthTime;
	}

	public override void _ValueChanged(double newValue)
	{
		FruitTreeBehaviorHandler.FruitGrowthTime = (float) newValue;
	}
}
