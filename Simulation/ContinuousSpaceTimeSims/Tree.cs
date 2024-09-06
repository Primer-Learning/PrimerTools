using Godot;
using PrimerAssets;
using PrimerTools;
using PrimerTools.Simulation;

public partial class Tree : Node3D
{
	// public Node3D Fruit;
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

		// var fruit = new MeshInstance3D();
		// fruit.Mesh = new SphereMesh();
		// fruit.Name = "Fruit";
		//
		// var mat = new StandardMaterial3D();
		// mat.AlbedoColor = PrimerColor.green;
		// fruit.Mesh.SurfaceSetMaterial(0, mat);
		//
		// AddChild(fruit);
		// // fruit.Owner = GetTree().EditedSceneRoot;
		//
		// fruit.Position = Vector3.Up;
		// Fruit = fruit;
	}
	
	public void DestroyFruit()
	{
		_fruitTree.GetFruit(0)?.QueueFree();
		// var fruit = GetNodeOrNull<MeshInstance3D>("Fruit");
		// fruit?.QueueFree();
		// Fruit = null;
	}

	public bool HasFruit => _fruitTree.GetFruit(0) != null;

}
