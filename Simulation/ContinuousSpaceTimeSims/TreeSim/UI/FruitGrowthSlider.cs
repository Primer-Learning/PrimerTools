using Godot;
using PrimerTools.Simulation;

public partial class FruitGrowthSlider : HSlider
{
	public override void _Ready()
	{
		base._Ready();
		Value = GetNode<FruitTreeSim>("%Tree Sim").FruitGrowthTime;
	}

	public override void _ValueChanged(double newValue)
	{
		GetNode<FruitTreeSim>("%Tree Sim").FruitGrowthTime = (float) newValue;
	}
}
