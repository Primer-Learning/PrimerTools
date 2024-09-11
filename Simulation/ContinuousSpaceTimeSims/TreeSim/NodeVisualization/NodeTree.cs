using Godot;
using PrimerTools.Simulation;
using PrimerTools.Simulation.TreeSim;

public partial class NodeTree : Node3D, IVisualTree
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

	public void CleanUp()
	{
		QueueFree();
	}

	public void UpdateTransform(PhysicalTree physicalTree)
	{
		Scale = Vector3.One * Mathf.Min(1, physicalTree.Age / FruitTreeSim.TreeMaturationTime);
	}

	public void Death()
	{
		Visible = false;
	}

	public void Initialize(PhysicalTree physicalTree)
	{
		Scale = Vector3.One * 0.5f; // Start as a sapling
		Position = physicalTree.Position;
		Name = "Tree";
	}
}
