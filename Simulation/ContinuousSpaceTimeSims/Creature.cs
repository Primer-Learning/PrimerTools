using Godot;
using PrimerTools;
using Blob = PrimerAssets.Blob;

public partial class Creature : Node3D
{
	private Blob _blob;

	public override void _Ready()
	{
		base._Ready();

		// var cube = new MeshInstance3D();
		// cube.Mesh = new BoxMesh();
		// AddChild(cube);

		_blob = Blob.CreateInstance();
		AddChild(_blob);
		// this.MakeSelfAndChildrenLocal();

		_blob.BlobAnimationTree.Active = false;
	}

	private bool _eating;
	public async void Eat()
	{
		if (_eating) return;
		_eating = true;
		await _blob.TriggerEat();
		_eating = false;
	}
}