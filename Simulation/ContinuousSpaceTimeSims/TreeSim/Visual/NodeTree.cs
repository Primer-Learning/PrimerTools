using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public partial class NodeTree : NodeEntity<DataTree>
{
	// Lazy thing. Gives visual trees an rng object for rotating mangoes.
	// Could pass the simulation world rng if we wanted to couple these to the results
	// Probably no need.
	public static Rng NodeTreeRng = new Rng(0);
	
	private FruitTree _fruitTree;
	
	#region Core methods
	public override void _Ready()
	{
		base._Ready();
		_fruitTree = FruitTree.CreateInstance();
		_fruitTree.Rng = NodeTreeRng;
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
                
		if (dataTree.FruitGrowthProgress > FruitTreeSimSettings.NodeFruitGrowthDelay && this is
		    {
			    HasFruit: false
		    })
		{
			GrowFruit(FruitTreeSimSettings.FruitGrowthTime - FruitTreeSimSettings.NodeFruitGrowthDelay);
		}
		Scale = Vector3.One * Mathf.Min(1, dataTree.Age / FruitTreeSimSettings.TreeMaturationTime);
	}
	private void Death()
	{
		Visible = false;
		QueueFree();
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
