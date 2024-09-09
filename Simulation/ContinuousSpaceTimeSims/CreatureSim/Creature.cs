using Godot;
using PrimerAssets;
using PrimerTools.Simulation;
using Blob = PrimerAssets.Blob;

public partial class Creature : Node3D, IEntity
{
	private Blob _blob;

	public override void _Ready()
	{
		base._Ready();

		_blob = Blob.CreateInstance();
		AddChild(_blob);
		_blob.BlobAnimationTree.Active = false;
		
		_blob.SetColor(PrimerColor.blue);
	}

	private bool _eating;
	public async void Eat(Node3D fruit)
	{
		if (_eating)
		{
			GD.Print("Attempting to eat while already eating.");
			return;
		}
		_eating = true;

		var tween = CreateTween();
		tween.TweenProperty(
			fruit,
			"scale",
			Vector3.Zero,
			0.5f
		);
		tween.Parallel();
		if (GlobalPosition.LengthSquared() < 1) GD.Print("Hrm"); 
		tween.TweenProperty(
			fruit,
			"global_position",
			GlobalPosition + _blob.MouthPosition,
			0.5f
		);

		await _blob.TriggerEat();
		_eating = false;
	}

	public void CleanUp()
	{
		QueueFree();
		Dispose();
	}
}
