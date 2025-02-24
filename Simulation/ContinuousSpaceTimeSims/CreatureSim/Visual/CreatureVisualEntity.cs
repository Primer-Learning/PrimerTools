using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;

public partial class CreatureVisualEntity : VisualEntity, IVisualEntityWithModel<ICreatureModelHandler>
{
    public ICreatureModelHandler ModelHandler { get; }

    public CreatureVisualEntity(ICreatureModelHandler modelHandler)
    {
        ModelHandler = modelHandler;
    }

    public override void _Ready()
    {
        base._Ready();
        Name = "Creature";
        ModelHandler.OnReady(this);
    }

    public override void Initialize(EntityRegistry registry, EntityId entityId)
    {
        base.Initialize(registry, entityId);

        if (registry.TryGetComponent<CreatureComponent>(entityId, out var creature))
        {
            var physicsComponent = registry.GetComponent<AreaPhysicsComponent>(entityId);
            Position = physicsComponent.Position;
            Scale = creature.Age / CreatureSimSettings.Instance.MaturationTime * Vector3.One;

            var normalizedAwareness = creature.AwarenessRadius / CreatureSimSettings.Instance.ReferenceAwarenessRadius;
            ModelHandler.Initialize(normalizedAwareness);
        }
    }

    public override void Update(EntityRegistry registry)
    {
        if (!registry.TryGetComponent<CreatureComponent>(EntityId, out var creature))
            return;

        UpdateTransform(registry, creature);
        ModelHandler.Update(creature);
    }

    public override void AddDebugNodes(AreaPhysicsComponent component)
    {
        throw new System.NotImplementedException();
    }
    
    private void UpdateTransform(EntityRegistry registry, CreatureComponent creature)
    {
        Scale = Vector3.One * (creature.ForcedMature
            ? 1
            : Mathf.Min(1, creature.Age / CreatureSimSettings.Instance.MaturationTime));

        if (creature.EatingTimeLeft > 0) return;

        var physicsComponent = registry.GetComponent<AreaPhysicsComponent>(creature.EntityId);
        
        Position = physicsComponent.Position;
        Quaternion = physicsComponent.Quaternion;
        
        // TODO: Make creatures update their rotation in the data layer
        // var direction = physicsComponent.Velocity;
        // if (direction.LengthSquared() > 0.0001f)
        // {
        //     LookAt(GlobalPosition - direction, Vector3.Up);
        // }
    }

    public async void HandleDeath(CreatureSystem.DeathCause cause)
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector3.Zero, 0.5f / SimulationWorld.TimeScale);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
    }

    public async void HandleEat(Node3D fruit, float eatDuration)
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
            GlobalPosition + GlobalBasis * ModelHandler.GetMouthPosition(),
            fruitMoveDuration / SimulationWorld.TimeScale
        );

        ModelHandler.TriggerEatAnimation((fruitMoveDuration + animationSettleDuration) / SimulationWorld.TimeScale);

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
}
