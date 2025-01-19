using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public partial class NodeTree : NodeEntity<DataTree>
{
	// Lazy thing. Gives visual trees an rng object for rotating mangoes.
	// Could pass an rng value if we wanted the visual part of the sim to be reliable.
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
		Scale = Vector3.Zero;
		Position = dataTree.Position;
		Rotation = new Vector3(0, dataTree.Angle, 0);
		Name = "Tree";
	}
	public override void Update(DataTree dataTree)
	{
		if (!dataTree.Alive)
		{
			Death();
			return;
		}
		
		if (dataTree is { HasFruit: false } tree && tree.FruitGrowthProgress > FruitTreeSimSettings.NodeFruitGrowthDelay)
		{
			GrowFruit(FruitTreeSimSettings.FruitGrowthTime - FruitTreeSimSettings.NodeFruitGrowthDelay);
		}

		Scale = ScaleFromAge(dataTree.Age);
	}

	public static Vector3 ScaleFromAge(float age)
	{
		return Vector3.One * Mathf.Min(1, age / FruitTreeSimSettings.TreeMaturationTime);
	}

	public Tween TweenToCorrectScale(float age, float duration)
	{
		var tween = CreateTween();
		tween.TweenProperty(
			this,
			"scale",
			ScaleFromAge(age),
			duration
		);
		return tween;
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
