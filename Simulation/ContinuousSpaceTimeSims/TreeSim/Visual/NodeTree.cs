using Godot;
using PrimerTools.Simulation;

public partial class NodeTree : NodeEntity<DataTree>
{
	private FruitTree _fruitTree;
	
	#region Core methods
	public override void _Ready()
	{
		base._Ready();
		_fruitTree = FruitTree.CreateInstance();
		_fruitTree.Rng = SimulationWorld.Rng;
		AddChild(_fruitTree);
	}
	public override void Initialize(DataTree dataTree)
	{
		Scale = Vector3.One * 0.5f; // Start as a sapling
		Position = dataTree.Position;
		Name = "Tree";
	}
	public override void Update(DataTree dataTree)
	{
		if (!dataTree.Alive)
		{
			Death();
			return;
		}
                
		if (dataTree.FruitGrowthProgress > FruitTreeBehaviorHandler.NodeFruitGrowthDelay && this is
		    {
			    HasFruit: false
		    })
		{
			GrowFruit(FruitTreeBehaviorHandler.FruitGrowthTime - FruitTreeBehaviorHandler.NodeFruitGrowthDelay);
		}
		Scale = Vector3.One * Mathf.Min(1, dataTree.Age / FruitTreeBehaviorHandler.TreeMaturationTime);
	}
	private void Death()
	{
		Visible = false;
	}
	#endregion

	#region Behaviors and helpers
	public void GrowFruit(double duration)
	{
		_fruitTree.GrowFruitTween(0, duration / SimulationWorld.TimeScale);
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