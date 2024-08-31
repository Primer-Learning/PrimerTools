using System.Reflection.Metadata;
using Godot;
using Blob = PrimerAssets.Blob;

public partial class Creature : Node3D
{
	public override void _Ready()
	{
		base._Ready();

		var blob = Blob.CreateInstance();
		AddChild(blob);

		// var body = new MeshInstance3D();
		// body.Mesh = new BoxMesh();
		// AddChild(body);
	}
}
