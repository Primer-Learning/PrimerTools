using System.Threading.Tasks;
using Godot;
using PrimerAssets;
using PrimerTools.Simulation;
using Blob = PrimerAssets.Blob;

public partial class NodeCreature : Node3D, IEntity
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
	
	public async void Eat(Node3D fruit, float eatDuration)
	{
		var turnDuration = 0.25f;
		var fruitMoveDuration = 0.5f;
		var animationSettleDuration = 0.5f;

		var durationFactor = eatDuration / (turnDuration + fruitMoveDuration + animationSettleDuration);
		if (durationFactor < 1)
		{
			turnDuration *= durationFactor;
			fruitMoveDuration *= durationFactor;
			animationSettleDuration *= durationFactor;
			// GD.Print("Compressing creature animation");
		}
		
		var differenceVector = fruit.GlobalPosition - GlobalPosition;
		var originalRotation = Quaternion;
		var intendedRotation =
			Quaternion.FromEuler(new Vector3(0, Mathf.Atan2(differenceVector.X, differenceVector.Z), 0));
		
		var tween = CreateTween();
		tween.TweenProperty(
			this,
			"quaternion",
			intendedRotation,
			turnDuration
		);
		await tween.ToSignal(tween, Tween.SignalName.Finished);
		
		var nextTween = CreateTween();
		nextTween.TweenProperty(
			fruit,
			"scale",
			Vector3.Zero,
			fruitMoveDuration
		);
		nextTween.Parallel();
		nextTween.TweenProperty(
			fruit,
			"global_position",
			GlobalPosition + GlobalBasis * _blob.MouthPosition,
			fruitMoveDuration
		);

		_blob.TriggerEat(fruitMoveDuration + animationSettleDuration);
		
		await nextTween.ToSignal(nextTween, Tween.SignalName.Finished);
		var finalTween = CreateTween();
		finalTween.TweenProperty(
			this,
			"quaternion",
			originalRotation,
			animationSettleDuration
		);
	}

	public void CleanUp()
	{
		QueueFree();
	}
}
