using Godot;
using PrimerTools;
using Blob = PrimerAssets.Blob;

public partial class Creature : Node3D
{
	private Blob _blob;

	public override void _Ready()
	{
		base._Ready();

		_blob = Blob.CreateInstance();
		AddChild(_blob);
		_blob.MakeSelfAndChildrenLocal();

		_blob.BlobAnimationTree.Active = true;
	}

	public void Eat()
	{
		_blob.TriggerEat();
	}
}
