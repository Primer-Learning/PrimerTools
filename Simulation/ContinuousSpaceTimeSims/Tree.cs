using Godot;
using PrimerAssets;
using PrimerTools;

public partial class Tree : Node3D
{
	public override void _Ready()
	{
		var mesh = new MeshInstance3D();
		mesh.Mesh = new CylinderMesh();
		AddChild(mesh);
		this.MakeSelfAndChildrenLocal();
	}
	
	public void AddFruit()
	{
		var fruit = new MeshInstance3D();
		fruit.Mesh = new SphereMesh();
		fruit.Name = "Fruit";

		var mat = new StandardMaterial3D();
		mat.AlbedoColor = PrimerColor.green;
		fruit.Mesh.SurfaceSetMaterial(0, mat);
		
		AddChild(fruit);
		fruit.Owner = GetTree().EditedSceneRoot;
		
		fruit.Position = Vector3.Up;
	}

	public void DestroyFruit()
	{
		var fruit = GetNodeOrNull<MeshInstance3D>("Fruit");
		fruit?.QueueFree();
	}
	
	
}
