using Godot;
using PrimerAssets;
using PrimerTools;
using PrimerTools.Simulation;
using Blob = PrimerAssets.Blob;

public partial class NodeCreature : Node3D, IVisualCreature
{
	private Blob _blob;

	public override void _Ready()
	{
		base._Ready();
		
		Name = "Creature"; 
		_blob = Blob.CreateInstance();
		AddChild(_blob);
		_blob.BlobAnimationTree.Active = false;
		
		_blob.SetColor(PrimerColor.Blue);
	}

	public void Initialize(PhysicalCreature physicalCreature)
	{
		Position = physicalCreature.Position;
		Scale = Vector3.Zero;
		
		if (_blob == null)
		{
			PrimerGD.PrintErrorWithStackTrace("Creature blob does not exist and cannot have its visuals adjusted.");
			return;
		}
		
		_blob.SetColor(ColorFromSpeed(physicalCreature.MaxSpeed));
		var normalizedAwareness = physicalCreature.AwarenessRadius / CreatureSim.InitialAwarenessRadius;
		_blob.LeftEye.Scale = normalizedAwareness * Vector3.One;
		_blob.RightEye.Scale = normalizedAwareness * Vector3.One;
	}

	private static Color[] _speedColors = new[]
	{
		PrimerColor.Black,
		PrimerColor.Purple,
		PrimerColor.Blue,
		PrimerColor.Green,
		PrimerColor.Yellow,
		PrimerColor.Orange,
		PrimerColor.Red,
		PrimerColor.White
	};
	public static Color ColorFromSpeed(float speed)
	{
		// TODO: Improve this algorithm
		var normalizedSpeed = speed / CreatureSim.InitialCreatureSpeed;
		normalizedSpeed /= 2; // Make the color range vary from zero speed to twice the initial speed

		var numSpaces = normalizedSpeed * (_speedColors.Length - 1); // Map the speed to a range that spans the colors
		var intSpaces = (int)numSpaces; // int
		var extra = numSpaces % 1; // fraction

		return PrimerColor.MixColorsByWeight(
			new [] {_speedColors[intSpaces], _speedColors[intSpaces + 1]},
			new [] { 1 - extra, extra}
		);
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
	public async void Death()
	{
		var tween = CreateTween();
		tween.TweenProperty(
			this,
			"scale",
			Vector3.Zero,
			0.5f
		);
		await tween.ToSignal(tween, Tween.SignalName.Finished);
		CleanUp();
	}

	public void UpdateTransform(PhysicalCreature physicalCreature)
	{
		var scaleFactor = Mathf.Min(1, physicalCreature.Age / CreatureSim.MaturationTime);
		Scale = scaleFactor * Vector3.One;
        
		if (physicalCreature.EatingTimeLeft > 0) return;
        
		// Position and rotation
		Position = physicalCreature.Position;
		var direction = physicalCreature.Velocity;
		if (direction.LengthSquared() > 0.0001f)
		{
			LookAt(GlobalPosition - direction, Vector3.Up);
		}
	}

	public void CleanUp()
	{
		QueueFree();
	}
}
