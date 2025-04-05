using Godot;
using PrimerTools.Simulation.Components;

namespace PrimerTools.Simulation.Visual;

public partial class TreeVisualEntity : VisualEntity 
{
    public override void Update(EntityRegistry registry)
    {
	    if (!registry.TryGetComponent<TreeComponent>(EntityId, out var tree))
		    return;
	    
	    if (tree is { HasFruit: false } && tree.FruitGrowthProgress > FruitTreeSimSettings.NodeFruitGrowthDelay)
	    {
		    GrowFruit(FruitTreeSimSettings.FruitGrowthTime - FruitTreeSimSettings.NodeFruitGrowthDelay);
	    }

	    Scale = ScaleFromAge(tree.Age);
    }

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
		Name = "Tree";
	}
	public override void Initialize(EntityRegistry registry, EntityId entityId)
	{
		base.Initialize(registry, entityId);
		
		if (registry.TryGetComponent<TreeComponent>(entityId, out var tree))
		{
			Scale = Vector3.Zero;
			Position = registry.GetComponent<AreaPhysicsComponent>(entityId).Position;
			Rotation = new Vector3(0, tree.Angle, 0);
		}
	}

	private static Vector3 ScaleFromAge(float age)
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
	
	public void Death()
	{
		// GD.Print($"Killing tree {Name}");
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