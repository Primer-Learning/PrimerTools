using System.Reflection.Metadata.Ecma335;
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
	
	public void GrowFruit(double duration)
	{
		// _fruitTree.GrowFruitAnimation(0);
		_fruitTree.GrowFruitTween(0, duration);
	}
	
	public void DestroyFruit()
	{
		var fruit = _fruitTree.GetFruit(0); 
		if (fruit == null) return;
		fruit.Scale = Vector3.Zero;
	}
	
	public Node3D GetFruit() 
	{
		return 	_fruitTree.GetFruit(0);
	}

	public bool HasFruit
	{
		get
		{
			var fruit = _fruitTree.GetFruit(0);
			return fruit != null && _fruitTree.IsFruitOnFlower(fruit);
		}
	}
}
