using System;
using System.Collections.Generic;
using System.Linq;
using GladiatorManager.ContinuousSpaceTimeSims.CreatureSim.Visual;
using Godot;
using PrimerTools.Simulation.Components;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public class CreatureSystem : ISystem, IVisualizedSystem
{
    private EntityRegistry _registry;
    private SimulationWorld _simulationWorld;
    
    public static event Action<EntityId, DeathCause> CreatureDeathEvent;
    public static event Action<EntityId, EntityId, float> CreatureEatEvent; // creatureId, treeId, duration
    public event Action Stepped;
    public IVisualEventManager CreateVisualEventManager(VisualEntityRegistry visualEntityRegistry)
    {
        return new CreatureVisualEventManager(visualEntityRegistry);
    }

    public void Initialize(EntityRegistry registry, SimulationWorld simulationWorld)
    {
        _registry = registry;
        _simulationWorld = simulationWorld;
    }

    public void CreateInitialPopulation(IEnumerable<Vector3> initialPositions = null)
    {
        List<Vector3> posList;
        if (initialPositions == null)
        {
            posList = new List<Vector3>();
            for (var i = 0; i < 100; i++) // We can make this configurable later
            {
                posList.Add(
                    new Vector3(
                        _simulationWorld.Rng.RangeFloat(_simulationWorld.WorldMin.X, _simulationWorld.WorldMax.X),
                        0,
                        _simulationWorld.Rng.RangeFloat(_simulationWorld.WorldMin.Y, _simulationWorld.WorldMax.Y)
                    )
                );
            }
        }
        else
        {
            posList = initialPositions.ToList();
        }

        var j = 0;
        foreach (var creature in CreatureSimSettings.Instance.InitializePopulation(posList.Count, _simulationWorld.Rng))
        {
            RegisterCreatureComponentAlongWithAPhysicsComponent(creature, posList[j]);
            j++;
        }
    }

    private void RegisterCreatureComponentAlongWithAPhysicsComponent(CreatureComponent creature, Vector3 position)
    {
        var entityId = _registry.CreateEntity();
            
        // Create physics component first
        var physicsComponent = new AreaPhysicsComponent(
            _simulationWorld.World3D.Space,
            position,
            new CapsuleShape3D { Height = 1.0f, Radius = 0.25f },
            new Vector3(0, 0.5f, 0)
        );
        physicsComponent.AddAwareness(
            _simulationWorld.World3D.Space,
            creature.Genome.GetTrait<float>("AwarenessRadius").ExpressedValue,
            new Vector3(0, 0.5f, 0)
        );
        _registry.AddComponent(entityId, physicsComponent);
        CollisionRegistry.RegisterBody(physicsComponent.GetBodyRid(), typeof(CreatureComponent), entityId);
            
        // Create creature component
        var creatureComponent = new CreatureComponent(creature.Genome) { CurrentDestination = position };
        _registry.AddComponent(entityId, creatureComponent);
    }

    public void Update(float deltaTime)
    {
        // Age update and death checks
        foreach (var immutableCreature in _registry.GetComponents<CreatureComponent>().ToArray())
        {
            var creature = immutableCreature;
            if (!creature.Alive) continue;
            creature.Age += deltaTime;
            
            var deathCause = CheckForDeath(creature, _simulationWorld.Rng);
            if (deathCause != DeathCause.None)
            {
                creature.Alive = false;
                CreatureDeathEvent?.Invoke(creature.EntityId, deathCause);
                _registry.DestroyEntity(creature.EntityId);
                continue;
            }
            var physicsComponent = _registry.GetComponent<AreaPhysicsComponent>(creature.EntityId);
            
            if (ProcessInternalStateAndCheckBusy(ref creature, deltaTime))
            {
                physicsComponent.Velocity = Vector3.Zero;
                _registry.UpdateComponent(physicsComponent);
                _registry.UpdateComponent(creature);
                continue;
            }
            
            var nearbyEntities = CollisionDetector.GetOverlappingEntitiesWithArea(
                physicsComponent.Awareness.Area,
                _simulationWorld.World3D.Space,
                physicsComponent.Body.Area);
            // Check for mating
            if (IsOpenToMating(creature))
            {
                // GD.Print("Mate?");
                var creatureCollisions = nearbyEntities
                    .Where(c => c.EntityType == typeof(CreatureComponent)).ToArray();
                
                if (creatureCollisions.Any())
                {
                    // GD.Print("Mate???");
                    var creaturesOpenToMating = creatureCollisions
                            .Select(c =>
                            {
                                var found = _registry.TryGetComponent<CreatureComponent>(c.EntityId, out var mateCreatureComponent);
                                return found ? mateCreatureComponent : default;
                            })
                            .Where(IsOpenToMating)
                            .ToList();
                    
                    if (creaturesOpenToMating.Any())
                    {
                        // GD.Print("Mate!!!!!");
                        var mate = creaturesOpenToMating
                            .MinBy(c =>
                                (
                                    _registry.GetComponent<AreaPhysicsComponent>(c.EntityId).Position -
                                    physicsComponent.Position
                                ).LengthSquared()
                            );
                        
                        var creaturePhysics = _registry.GetComponent<AreaPhysicsComponent>(creature.EntityId);
                        var matePhysics = _registry.GetComponent<AreaPhysicsComponent>(mate.EntityId);
                        if ((matePhysics.Position - creaturePhysics.Position)
                            .IsLengthLessThan(CreatureSimSettings.Instance.CreatureMateDistance))
                        {
                            var mutableMate = mate;
                            mutableMate.MatingTimeLeft += CreatureSimSettings.Instance.ReproductionDuration;
                            creature.MatingTimeLeft += CreatureSimSettings.Instance.ReproductionDuration;
                            mutableMate.Energy -= CreatureSimSettings.Instance.ReproductionEnergyCost / 2;
                            creature.Energy -= CreatureSimSettings.Instance.ReproductionEnergyCost / 2;
                            
                            var offspring = new CreatureComponent(
                                CreatureSimSettings.Instance
                                    .Reproduce(creature.Genome, mate.Genome, _simulationWorld.Rng).Genome
                            );
                            RegisterCreatureComponentAlongWithAPhysicsComponent(
                                offspring,
                                (matePhysics.Position + creaturePhysics.Position) / 2
                            );
                            
                            _registry.UpdateComponent(creature);
                            _registry.UpdateComponent(mutableMate);
                            continue;
                        }

                        creature.CurrentDestination = matePhysics.Position;
                        PerformMovement(ref creature, deltaTime);
                        _registry.UpdateComponent(creature);
                        continue;
                    }
                }
            }

            // Check for eating
            if (creature.Energy < creature.HungerThreshold)
            {
                var treeCollisions = nearbyEntities.Where(c => c.EntityType == typeof(TreeComponent)).ToArray();
                if (treeCollisions.Any())
                {
                    var treesWithFruit = treeCollisions
                        .Select(c =>
                        {
                            var found = _registry.TryGetComponent<TreeComponent>(c.EntityId, out var treeComponent);
                            return found ? treeComponent : default;
                        })
                        .Where(t => t.HasFruit)
                        .ToList();

                    if (treesWithFruit.Any())
                    {
                        var closestTree = treesWithFruit
                            .MinBy(t =>
                                {
                                    var treePhysics = _registry.GetComponent<AreaPhysicsComponent>(t.EntityId);
                                    return (treePhysics.Position - physicsComponent.Position).LengthSquared();
                                }
                            );

                        var closestTreePhysicsComponent = _registry.GetComponent<AreaPhysicsComponent>(closestTree.EntityId); 
                        
                        if ((closestTreePhysicsComponent.Position - physicsComponent.Position)
                            .IsLengthLessThan(CreatureSimSettings.Instance.CreatureEatDistance)
                            && creature.EatingTimeLeft <= 0)
                        {
                            creature.Digesting += _simulationWorld.Rng.RangeFloat(
                                CreatureSimSettings.Instance.MinEnergyGainFromFood,
                                CreatureSimSettings.Instance.MaxEnergyGainFromFood);
                            creature.EatingTimeLeft = CreatureSimSettings.Instance.EatDuration;
                            
                            var mutableTree = closestTree;
                            mutableTree.HasFruit = false;
                            mutableTree.FruitGrowthProgress = 0;
                            _registry.UpdateComponent(mutableTree);
                            
                            CreatureEatEvent?.Invoke(creature.EntityId, 
                                closestTree.EntityId, 
                                CreatureSimSettings.Instance.EatDuration / _simulationWorld.TimeScaleControl);
                            
                            _registry.UpdateComponent(creature);
                            continue;
                        }

                        creature.CurrentDestination = closestTreePhysicsComponent.Position;
                        PerformMovement(ref creature, deltaTime);
                        _registry.UpdateComponent(creature);
                        continue;
                    }
                }
            }

            // Random movement
            UpdateRandomDestinationIfNeeded(ref creature);
            PerformMovement(ref creature, deltaTime);
            
            _registry.UpdateComponent(creature);
        }
        Stepped?.Invoke();
    }
    
    
    #region Commands
    private void PerformMovement(ref CreatureComponent creature, float timeStep)
    {
        var physicsComponent = _registry.GetComponent<AreaPhysicsComponent>(creature.EntityId);
        
        physicsComponent.Velocity = Transform3DUtils.CalculateVelocityAcceleratedTowardTarget(
            creature.CurrentDestination,
            physicsComponent.Position,
            physicsComponent.Velocity,
            creature.MaxSpeed
        );
        
        SpendMovementEnergy(ref creature);
        _registry.UpdateComponent(physicsComponent);
    }
    private void SpendMovementEnergy(ref CreatureComponent creature)
    {
        var normalizedSpeed = creature.MaxSpeed / CreatureSimSettings.Instance.ReferenceCreatureSpeed;
        var normalizedAwarenessRadius = creature.AwarenessRadius / CreatureSimSettings.Instance.ReferenceAwarenessRadius;
        
        var energyCost = (CreatureSimSettings.Instance.BaseEnergySpend +
                          CreatureSimSettings.Instance.GlobalEnergySpendAdjustmentFactor *
                          (normalizedSpeed * normalizedSpeed + normalizedAwarenessRadius))
                         / SimulationWorld.PhysicsStepsPerSimSecond;
        
        creature.Energy -= energyCost;
    }
    private void UpdateRandomDestinationIfNeeded(ref CreatureComponent creature)
    {
        var position = _registry.GetComponent<AreaPhysicsComponent>(creature.EntityId).Position;
        if ((creature.CurrentDestination - position).LengthSquared() < 
            CreatureSimSettings.Instance.CreatureEatDistance * CreatureSimSettings.Instance.CreatureEatDistance)
        {
            creature.CurrentDestination = _simulationWorld.GetRandomDestination(
                position, 
                CreatureSimSettings.Instance.CreatureStepMaxLength);
        }
    }
    private bool ProcessInternalStateAndCheckBusy(ref CreatureComponent creature, float timeStep)
    {
        if (creature.Digesting > 0)
        {
            // Could be a DigestionRate setting or even trait?
            var digestAmount = Mathf.Min(creature.Digesting, 0.05f);
            creature.Energy += digestAmount;
            creature.Digesting -= digestAmount;
        }
        
        // Check maturation
        if (creature.Age < CreatureSimSettings.Instance.MaturationTime) return true;

        // Process eating state
        if (creature.EatingTimeLeft > 0)
        {
            creature.EatingTimeLeft -= timeStep;
            return true;
        }

        // Process mating state
        if (creature.MatingTimeLeft > 0)
        {
            creature.MatingTimeLeft = Mathf.Max(0, creature.MatingTimeLeft - timeStep);
            return true;
        }

        return false;
    }
    #endregion
    
    #region Queries
    private bool IsOpenToMating(CreatureComponent creature)
    {
        if (!creature.Alive) return false;
        if (creature.Energy < CreatureSimSettings.Instance.ReproductionEnergyThreshold) return false;
        if (creature.MatingTimeLeft > 0) return false;
        var maxReproductionAge = creature.Genome.GetTrait<float>("MaxReproductionAge");
        if (maxReproductionAge != null && creature.Age > maxReproductionAge.ExpressedValue) return false;
        
        return true;
    }
    private DeathCause CheckForDeath(CreatureComponent creature, Rng rng)
    {
        if (!creature.Alive) return DeathCause.None;

        // Check for starvation
        if (creature.Energy < 0)
        {
            return DeathCause.Starvation;
        }
            
        // Check for deaths from max age trait
        var maxAgeTrait = creature.Genome.GetTrait<float>("MaxAge");
        if (maxAgeTrait != null && maxAgeTrait.ExpressedValue < creature.Age)
        {
            return DeathCause.Aging;
        }
            
        // Check for death from deleterious mutations
        foreach (var trait in creature.Genome.Traits.Values)
        {
            if (trait is DeleteriousTrait deleteriousTrait)
            {
                if (deleteriousTrait.CheckForDeath(creature.Age, rng))
                {
                    return DeathCause.Aging;
                }
            }
        }
        
        // Deaths from antagonistic pleiotropy
        var apTrait = creature.Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
        if (apTrait is { ExpressedValue: true } && creature.Age > CreatureSimSettings.Instance.MaturationTime)
        {
            var apDeathRate = 0.03f;
            if (rng.rand.NextDouble() < 1 - Mathf.Pow(1 - apDeathRate, 1f / SimulationWorld.PhysicsStepsPerSimSecond))
            {
                return DeathCause.Aging;
            }
        }

        return DeathCause.None;
    }
    public enum DeathCause
    {
        None,
        Starvation,
        Aging
    }
    #endregion
    
}
