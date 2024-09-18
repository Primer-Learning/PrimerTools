using Godot;
using PrimerTools.Simulation;

public partial class NodeTree : Node3D, IEntity
{
	private FruitTree _fruitTree;
	
	#region Core methods
	// Considering putting these methods in an interface
	public override void _Ready()
	{
		base._Ready();
		_fruitTree = FruitTree.CreateInstance();
		_fruitTree.Rng = SimulationWorld.Rng;
		AddChild(_fruitTree);
	}
	public void Initialize(DataTree dataTree)
	{
		Scale = Vector3.One * 0.5f; // Start as a sapling
		Position = dataTree.Position;
		Name = "Tree";
	}
	public void UpdateTransform(DataTree dataTree)
	{
		Scale = Vector3.One * Mathf.Min(1, dataTree.Age / FruitTreeBehaviorHandler.TreeMaturationTime);
	}
	public void Death()
	{
		Visible = false;
	}
	public void CleanUp()
	{
		QueueFree();
	}
	#endregion

	#region Behaviors and helpers
	public void GrowFruit(double duration)
	{
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
	#endregion
}
