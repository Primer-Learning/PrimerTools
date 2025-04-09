using System;
using System.Linq;
using Godot;
using PrimerTools.Simulation.Components;
using PrimerTools.Simulation.Visual;

namespace PrimerTools.Simulation;

public class FruitSystem : ISystem, IVisualizedSystem
{
    private EntityRegistry _registry;
    private SimulationWorld _simulationWorld;
    
    // Events
    public static event Action<EntityId> FruitCreatedEvent;
    public static event Action<EntityId> FruitRipenedEvent;
    public static event Action<EntityId> FruitDetachedEvent;
    public static event Action<EntityId> FruitDecayedEvent;
    public event Action Stepped;

    public void Initialize(EntityRegistry registry, SimulationWorld simulationWorld)
    {
        _registry = registry;
        _simulationWorld = simulationWorld;
    }

    public void Update(float deltaTime)
    {
        var fruitComponents = _registry.GetComponents<FruitComponent>().ToArray();
        
        foreach (var immutableFruit in fruitComponents)
        {
            var fruit = immutableFruit;
            
            // Update age
            // fruit.Age += deltaTime;
            
            if (fruit.IsAttached)
            {
                // Update growth for attached fruits
                UpdateAttachedFruit(ref fruit, deltaTime);
            }
            else
            {
                // Update detached fruits
                if (!UpdateDetachedFruit(ref fruit, deltaTime))
                {
                    continue;
                }
            }
            
            _registry.UpdateComponent(fruit);
        }
        
        // Check trees for new fruit opportunities
        // CheckTreesForFruitCreation(deltaTime);
        
        Stepped?.Invoke();
    }
    
    private void UpdateAttachedFruit(ref FruitComponent fruit, float deltaTime)
    {
        // Update growth progress
        if (fruit.GrowthProgress < 1.0f)
        {
            fruit.GrowthProgress += deltaTime / FruitTreeSimSettings.FruitGrowthTime;
            
            // Check if fruit just ripened
            if (fruit.GrowthProgress >= 1.0f)
            {
                fruit.GrowthProgress = 1.0f;
                FruitRipenedEvent?.Invoke(fruit.EntityId);
            }
        }
        else
        {
            // Chance for ripe fruit to fall
            if (_simulationWorld.Rng.RangeFloat(0, 1) < FruitTreeSimSettings.RipeFruitFallProbabilityPerSecond * deltaTime)
            {
                DetachFruit(ref fruit);
            }
        }
        
        // Update fruit position based on parent tree
        // if (_registry.TryGetComponent<TreeComponent>(fruit.ParentTreeId, out var tree))
        // {
        //     UpdateFruitPosition(ref fruit, tree);
        // }
    }
    
    /// <summary>
    /// Checks for decay. If the fruit decays, return false to signal it no longer exists.
    /// </summary>
    /// <param name="fruit"></param>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    private bool UpdateDetachedFruit(ref FruitComponent fruit, float deltaTime)
    {
        fruit.DetachedTime += deltaTime;
        
        if (!(fruit.DetachedTime >= FruitTreeSimSettings.FruitDecayTime)) return true;
        
        // Plant a tree if the mango is inside the world.
        if (SimulationWorld.Instance.IsWithinWorldBounds(fruit.Body.Transform.Origin))
        {
            TreeSystem.RegisterAndPlaceTreeEntity(
                new TreeComponent(),
                new Transform3D(
                    Basis.Identity.Rotated(Vector3.Up, _simulationWorld.Rng.RangeFloat(0, Mathf.Tau)),
                    fruit.Body.Transform.Origin
                )
            );
        }
            
        FruitDecayedEvent?.Invoke(fruit.EntityId);
        _registry.DestroyEntity(fruit.EntityId);
            
        return false;

    }
    
    private void DetachFruit(ref FruitComponent fruit)
    {
        fruit.IsAttached = false;
        
        // Update the parent tree to remove this fruit reference
        if (_registry.TryGetComponent<TreeComponent>(fruit.ParentTreeId, out var tree))
        {
            tree.AttachedFruits[fruit.PositionIndex] = new EntityId();
            _registry.UpdateComponent(tree);
        }
        
        // Enable physics for the fruit
        PhysicsServer3D.BodySetMode(fruit.Body.Rid, PhysicsServer3D.BodyMode.Rigid);
        PhysicsServer3D.BodyApplyCentralImpulse(fruit.Body.Rid, 
            new Vector3(_simulationWorld.Rng.RangeFloat(-0.5f, 0.5f), 0, _simulationWorld.Rng.RangeFloat(-0.5f, 0.5f)));
        
        FruitDetachedEvent?.Invoke(fruit.EntityId);
    }

    public IVisualEventManager CreateVisualEventManager(VisualEntityRegistry visualEntityRegistry)
    {
        return new FruitVisualEventManager(visualEntityRegistry);
    }
}
