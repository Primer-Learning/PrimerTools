using Godot;
using PrimerAssets;
using Blob = PrimerAssets.Blob;

namespace PrimerTools.Simulation.New;

public partial class NodeCreature : NodeEntity<DataCreature>
{
	private Blob _blob;

	#region Core methods
	public override void _Ready()
	{
		base._Ready();
		
		Name = "Creature"; 
		_blob = Blob.CreateInstance();
		AddChild(_blob);
		_blob.BlobAnimationTree.Active = false;
		
		_blob.SetColor(PrimerColor.Blue);
	}
	public override void Initialize(DataCreature dataCreature)
	{
		Position = dataCreature.Position;
		Scale = dataCreature.Age / CreatureSimSettings.Instance.MaturationTime * Vector3.One;
		
		if (_blob == null)
		{
			PrimerGD.PrintErrorWithStackTrace("Creature blob does not exist and cannot have its visuals adjusted.");
			return;
		}
		
		_blob.SetColor(ColorFromSpeed(dataCreature.MaxSpeed));
		var normalizedAwareness = dataCreature.AwarenessRadius / CreatureSimSettings.Instance.ReferenceAwarenessRadius;
		_blob.LeftEye.Scale = normalizedAwareness * Vector3.One;
		_blob.RightEye.Scale = normalizedAwareness * Vector3.One;
		// this.MakeSelfAndChildrenLocal();
	}

	public override void Update(DataCreature dataCreature)
	{
		UpdateTransform(dataCreature);
	}
	
	public void UpdateTransform(DataCreature dataCreature)
	{
		var scaleFactor = Mathf.Min(1, dataCreature.Age / CreatureSimSettings.Instance.MaturationTime);
		Scale = scaleFactor * Vector3.One;
        
		if (dataCreature.EatingTimeLeft > 0) return;
        
		// Position and rotation
		Position = dataCreature.Position;
		var direction = dataCreature.Velocity;
		if (direction.LengthSquared() > 0.0001f)
		{
			LookAt(GlobalPosition - direction, Vector3.Up);
		}
	}
	public async void Death()
	{
		var tween = CreateTween();
		tween.TweenProperty(
			this,
			"scale",
			Vector3.Zero,
			0.5f / SimulationWorld.TimeScale
		);
		await tween.ToSignal(tween, Tween.SignalName.Finished);
		QueueFree();
	}
	#endregion

	#region Behaviors and helpers
	private static Color[] _speedColors = 
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
		// Map the speed to a number between 0 and 1, with the reference speed mapping to 0.5
		var normalizedSpeed = speed / CreatureSimSettings.Instance.ReferenceCreatureSpeed / 2;
		normalizedSpeed = Mathf.Clamp(normalizedSpeed, 0, 1);

		if (normalizedSpeed <= 0) return _speedColors[0];
		if (normalizedSpeed >= 1) return _speedColors[^1];

		var adjustedT = normalizedSpeed * (_speedColors.Length - 1);
		var index = (int)adjustedT;
		var fraction = adjustedT - index;

		return PrimerColor.MixColorsByWeight(
			new[] { _speedColors[index], _speedColors[index + 1] },
			new[] { 1 - fraction, fraction }
		);
	}
	
	public async void Eat(Node3D fruit, float eatDuration)
	{
		if (Scale == Vector3.Zero) return;
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
			turnDuration / SimulationWorld.TimeScale
		);
		await tween.ToSignal(tween, Tween.SignalName.Finished);
		
		var nextTween = CreateTween();
		nextTween.TweenProperty(
			fruit,
			"scale",
			Vector3.Zero,
			fruitMoveDuration / SimulationWorld.TimeScale
		);
		nextTween.Parallel();
		nextTween.TweenProperty(
			fruit,
			"global_position",
			GlobalPosition + GlobalBasis * _blob.MouthPosition,
			fruitMoveDuration / SimulationWorld.TimeScale
		);

		_blob.TriggerEat(fruitMoveDuration + animationSettleDuration);
		
		await nextTween.ToSignal(nextTween, Tween.SignalName.Finished);
		var finalTween = CreateTween();
		finalTween.TweenProperty(
			this,
			"quaternion",
			originalRotation,
			animationSettleDuration / SimulationWorld.TimeScale
		);
	}
	#endregion
}
