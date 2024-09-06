using Godot;
using PrimerAssets;
using PrimerTools;
using PrimerTools.Simulation;

public partial class Tree : Node3D
{
	private FruitTree _fruitTree;
	public override void _Ready()
	{
		base._Ready();
		_fruitTree = FruitTree.CreateInstance();
		_fruitTree.Rng = SimulationWorld.Rng;
		AddChild(_fruitTree);
		// this.MakeSelfAndChildrenLocal();
	}
	
	public void AddFruit()
	{
		_fruitTree.GrowFruit(0);
	}
	
	public void DestroyFruit()
	{
		_fruitTree.GetFruit(0)?.QueueFree();
	}

	public bool HasFruit => _fruitTree.GetFruit(0) != null;

}
