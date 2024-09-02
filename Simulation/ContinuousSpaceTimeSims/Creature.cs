using Godot;
using Blob = PrimerAssets.Blob;

public partial class Creature : Node3D
{
	private Blob _blob;

	public override void _Ready()
	{
		base._Ready();

		_blob = Blob.CreateInstance();
		AddChild(_blob);
	}
}
