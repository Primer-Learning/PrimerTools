using Godot;
using PrimerAssets;
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
		// this.MakeSelfAndChildrenLocal();

		_blob.BlobAnimationTree.Active = false;
		
		_blob.SetColor(PrimerColor.blue);
	}

	private bool _eating;
	public async void Eat()
	{
		if (_eating)
		{
			GD.Print("Attempting to eat while already eating.");
			return;
		}
		_eating = true;
		await _blob.TriggerEat();
		_eating = false;
	}
}
