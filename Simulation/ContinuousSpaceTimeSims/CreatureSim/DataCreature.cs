using System;
using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public struct DataCreature : IDataEntity, IPhysicsEntity
{
    public int EntityId { get; set; }
    public bool Alive { get; set; }
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 CurrentDestination;
    public BodyComponent Body;
    public AwarenessComponent Awareness;
    public Genome Genome { get; set; }
    
    // Properties to easily access expressed trait values
    public float MaxSpeed => Genome.GetTrait<float>("MaxSpeed").ExpressedValue;
    public float AdjustedSpeed
    {
        get
        {
            var antagonisticPleiotropy = Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
            var factor = 1f;
            if (antagonisticPleiotropy is { ExpressedValue: true }) factor = 2f;
            return MaxSpeed * factor;
        }
    }
    
    public float AwarenessRadius => Genome.GetTrait<float>("AwarenessRadius").ExpressedValue;
    
    public float Age;
    public bool ForcedMature;
    public float Energy;
    public float Digesting;
    public float HungerThreshold;
    public int FoodTargetIndex;
    public float EatingTimeLeft;
    public float MatingTimeLeft;

    public bool OpenToMating
    {
        get
        {
            if (Energy < CreatureSimSettings.Instance.ReproductionEnergyThreshold) return false;
            if (MatingTimeLeft > 0) return false;
            var maxReproductionAge = Genome.GetTrait<float>("MaxReproductionAge");
            if (maxReproductionAge != null && Age > maxReproductionAge.ExpressedValue) return false;
            
            return true;
        }
    }
    
    public void CleanUp()
    {
        Body.CleanUp();
        Awareness.CleanUp();
    }

    public void Initialize(Rid space)
    {
        var shape = new CapsuleShape3D { Height = 1.0f, Radius = 0.25f };
        Body.Initialize(space, Position, shape);
        Awareness.Initialize(space, Position, AwarenessRadius);
        CurrentDestination = Position;
        Velocity = Vector3.Zero;
        Alive = true;
        Energy = 1f;
        HungerThreshold = CreatureSimSettings.Instance.HungerThreshold;
        FoodTargetIndex = -1;
    }

    public Rid GetBodyRid() => Body.Area;

    public void PerformMovement(float timeStep)
    {
        Velocity = UpdateVelocity();
        Position += Velocity * timeStep;
        
        var transform = Transform3D.Identity.Translated(Position);
        Body.UpdateTransform(transform);
        Awareness.UpdateTransform(transform);
        SpendMovementEnergy();
    }
    
    private void SpendMovementEnergy()
    {
        var normalizedSpeed = MaxSpeed / CreatureSimSettings.Instance.ReferenceCreatureSpeed;
        var normalizedAwarenessRadius = AwarenessRadius / CreatureSimSettings.Instance.ReferenceAwarenessRadius;
		
		
        var energyCost = (CreatureSimSettings.Instance.BaseEnergySpend +
                          CreatureSimSettings.Instance.GlobalEnergySpendAdjustmentFactor *
                          (normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius))
                         / SimulationWorld.PhysicsStepsPerSimSecond;
	
        // Aging lowers efficiency section
        // foreach (var trait in Genome.Traits.Values)
        // {
        //     if (trait is DeleteriousTrait { ExpressedValue: true, MortalityRatePerSecond: > 0 })
        //     {
        // 	    energyCost *= 1 + Age / 100f;
        //     }
        // }
		
        Energy -= energyCost;
    }

    public enum DeathCause
    {
        Starvation,
        Aging,
        None
    }

    public DeathCause CheckForDeath(Rng rng)
    {
        if (!Alive) return DeathCause.None;

        // Check for starvation
        if (Energy < 0)
        {
            Alive = false;
            return DeathCause.Starvation;
        }
            
        // Check for deaths from max age trait
        var maxAgeTrait = Genome.GetTrait<float>("MaxAge");
        if (maxAgeTrait != null && maxAgeTrait.ExpressedValue < Age)
        {
            Alive = false;
            return DeathCause.Aging;
        }
            
        // Check for death from deleterious mutations
        foreach (var trait in Genome.Traits.Values)
        {
            if (trait is DeleteriousTrait deleteriousTrait)
            {
                if (deleteriousTrait.CheckForDeath(Age, rng))
                {
                    Alive = false;
                    return DeathCause.Aging;
                }
            }
        }

        // Deaths from antagonistic pleiotropy
        var apTrait = Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
        if (apTrait is { ExpressedValue: true } && Age > CreatureSimSettings.Instance.MaturationTime)
        {
            var apDeathRate = 0.03f;
            if (rng.rand.NextDouble() < 1 - Mathf.Pow(1 - apDeathRate, 1f / SimulationWorld.PhysicsStepsPerSimSecond))
            {
                Alive = false;
                return DeathCause.Aging;
            }
        }

        return DeathCause.None;
    }

    public bool InternalProcessAndCheckBusyState(float timeStep)
    {
        if (Digesting > 0)
        {
            // Could be a DigestionRate setting or even trait?
            var digestAmount = Mathf.Min(Digesting, 0.05f);
            Energy += digestAmount;
            Digesting -= digestAmount;
        }
        
        // Check maturation
        if (Age < CreatureSimSettings.Instance.MaturationTime) 
            return true;

        // Process eating state
        if (EatingTimeLeft > 0)
        {
            EatingTimeLeft -= timeStep;
            return true;
        }

        // Process mating state
        if (MatingTimeLeft > 0)
        {
            MatingTimeLeft = Mathf.Max(0, MatingTimeLeft - timeStep);
            return true;
        }

        return false;
    }

    public void UpdateRandomDestinationIfNeeded(SimulationWorld world)
    {
        if ((CurrentDestination - Position).LengthSquared() < 
            CreatureSimSettings.Instance.CreatureEatDistance * CreatureSimSettings.Instance.CreatureEatDistance)
        {
            CurrentDestination = world.GetRandomDestination(Position, CreatureSimSettings.Instance.CreatureStepMaxLength);
        }
    }

    private Vector3 UpdateVelocity(float accelerationFactor = 0.1f)
    {
        if (CurrentDestination == Vector3.Zero) GD.Print("Moving to the origin");
        var desiredDisplacement = CurrentDestination - Position;
        var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
        
        // Calculate desired velocity
        var desiredVelocity = desiredDisplacement * MaxSpeed / Mathf.Sqrt(desiredDisplacementLengthSquared);
        
        // Calculate velocity change
        var velocityChange = desiredVelocity - Velocity;
        var velocityChangeLengthSquared = velocityChange.LengthSquared();

        // Calculate acceleration vector with a maximum magnitude
        var maxAccelerationMagnitudeSquared = MaxSpeed * MaxSpeed * accelerationFactor * accelerationFactor;
        Vector3 accelerationVector;
        if (velocityChangeLengthSquared > maxAccelerationMagnitudeSquared)
        {
            accelerationVector = Mathf.Sqrt(maxAccelerationMagnitudeSquared / velocityChangeLengthSquared) * velocityChange;
        }
        else
        {
            accelerationVector = velocityChange;
        }

        var newVelocity = Velocity + accelerationVector;
        
        // Limit velocity to max speed
        var velocityLengthSquared = newVelocity.LengthSquared();
        var maxSpeedSquared = MaxSpeed * MaxSpeed;
        if (velocityLengthSquared > maxSpeedSquared)
        {
            newVelocity = MaxSpeed / Mathf.Sqrt(velocityLengthSquared) * newVelocity;
        }

        return newVelocity;
    }
}
