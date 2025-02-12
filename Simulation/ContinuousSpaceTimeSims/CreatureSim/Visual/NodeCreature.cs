using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;

public partial class NodeCreature : NodeEntity
{
    private readonly ICreatureFactory _creatureFactory;
    private ICreatureModelHandler _creatureModelHandler;
    // private StatusDisplay _statusDisplay;

    public NodeCreature(ICreatureFactory creatureFactory)
    {
        _creatureFactory = creatureFactory;
    }

    public NodeCreature()
    {
	    throw new System.NotImplementedException();
    }

    #region Core methods
    public override void _Ready()
    {
        base._Ready();
        Name = "Creature";

        _creatureModelHandler = _creatureFactory.CreateInstance();
        _creatureModelHandler.OnReady(this);
	}
	public override void Initialize(IDataEntity dataEntity)
	{
		var creature = (DataCreature)dataEntity;
		Position = creature.Position;
		Scale = creature.Age / CreatureSimSettings.Instance.MaturationTime * Vector3.One;
		
		if (_creatureModelHandler == null)
		{
			PrimerGD.PrintErrorWithStackTrace("Creature blob does not exist and cannot have its visuals adjusted.");
			return;
		}
		
		var normalizedAwareness = creature.AwarenessRadius / CreatureSimSettings.Instance.ReferenceAwarenessRadius;
		_creatureModelHandler.Initialize(normalizedAwareness);
		
		// TODO: Repair the status display scene
		// _statusDisplay = StatusDisplay.CreateInstance();
		// _blob.AddChild(_statusDisplay);
		// _statusDisplay.Position = Vector3.Up * 2;
		// _statusDisplay.Scale = Vector3.One;
		// _statusDisplay.Energy = dataCreature.Energy;
		// this.MakeSelfAndChildrenLocal();
	}

	public override void Update(IDataEntity dataEntity)
	{
		var creature = (DataCreature)dataEntity;
		UpdateTransform(creature);
		_creatureModelHandler.Update(creature);
		
		// Display
		// _statusDisplay.Energy = dataCreature.Energy;
	}
	
	public void UpdateTransform(DataCreature dataCreature)
	{
		Scale = Vector3.One * (dataCreature.ForcedMature
				? 1
				: Mathf.Min(1, dataCreature.Age / CreatureSimSettings.Instance.MaturationTime)
			);
        
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
			GlobalPosition + GlobalBasis * _creatureModelHandler.GetMouthPosition(),
			fruitMoveDuration / SimulationWorld.TimeScale
		);

		_creatureModelHandler.TriggerEatAnimation((fruitMoveDuration + animationSettleDuration) / SimulationWorld.TimeScale);
		
		await nextTween.ToSignal(nextTween, Tween.SignalName.Finished);
		fruit.QueueFree();
		
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
