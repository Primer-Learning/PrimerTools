using Godot;
using System;

public partial class Tree : Node3D
{
	public override void _Ready()
	{
		var mesh = new MeshInstance3D();
		mesh.Mesh = new CylinderMesh();
		
		AddChild(mesh);
	}
	
	public void AddFruit()
	{
		var mesh = new MeshInstance3D();
		mesh.Mesh = new SphereMesh();
		mesh.Name = "Fruit";
		
		AddChild(mesh);
	}

	public void DestroyFruit()
	{
		var fruit = GetNodeOrNull<MeshInstance3D>("Fruit");
		fruit?.QueueFree();
	}
	
	
}
