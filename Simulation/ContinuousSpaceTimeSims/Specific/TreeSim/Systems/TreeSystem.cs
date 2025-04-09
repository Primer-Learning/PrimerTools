using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GladiatorManager.ContinuousSpaceTimeSims.CreatureSim.Visual;
using Godot;
using PrimerTools.Simulation.Components;
using PrimerTools.Utilities;

namespace PrimerTools.Simulation;

public class TreeSystem : ISystem, IVisualizedSystem
{
    private EntityRegistry _registry;
    private SimulationWorld _simulationWorld;
    
    public static event Action<EntityId> TreeDeathEvent;
    public event Action Stepped;
    
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
            for (var i = 0; i < 30; i++) // We can make this configurable later
            {
                posList.Add(VectorUtilities.RandomVector3(_simulationWorld.WorldMin, _simulationWorld.WorldMax, _simulationWorld.Rng));
            }
        }
        else
        {
            posList = initialPositions.ToList();
        }

        foreach (var pos in posList)
        {
            var newTree = new TreeComponent
            {
                Age = FruitTreeSimSettings.TreeMaturationTime,
                // Randomize the spawn time so they don't all reproduce in sync
                TimeSinceLastSpawn = _simulationWorld.Rng.RangeFloat(0, FruitTreeSimSettings.TreeSpawnInterval)
            };
            RegisterAndPlaceTreeEntity(
                newTree,
                new Transform3D(
                    Basis.Identity.Rotated(Vector3.Up, _simulationWorld.Rng.RangeFloat(0, Mathf.Tau)),
                    pos
                )
            );
        }
    }

    public static TreeComponent RegisterAndPlaceTreeEntity(TreeComponent treeComponent, Transform3D transform)
    {
        var entityId = EntityRegistry.Instance.CreateEntity();
        
        // Ground position. Just make y zero for now. Eventually probably raycast to find the ground.
        var groundedPosition = new Vector3(
            transform.Origin.X,
            0,
            transform.Origin.Z
        );
        
        treeComponent.Body = new BodyHandler(
            SimulationWorld.Instance.World3D.Space,
            new Transform3D(
                transform.Basis,
                groundedPosition
            ),
            Transform3D.Identity,
            new SphereShape3D { Radius = 1.0f }
        );
        
        CollisionRegistry.RegisterBody(treeComponent.Body.Rid, typeof(TreeComponent), entityId);
        EntityRegistry.Instance.AddComponent(entityId, treeComponent);

        return treeComponent;
    }

    public void Update(float deltaTime)
    {
        var newTreePositions = new List<Vector3>();
        var treesCopy = _registry.GetComponents<TreeComponent>().ToArray();
        foreach (var immutableTree in treesCopy)
        {
            var tree = immutableTree;
            if (!tree.Alive)
            {
                if (treesThatShouldBeGone.Contains(tree.EntityId))
                {
                    GD.Print($"I swear I killed tree {tree.EntityId.Value}.");
                }
                continue;
            }
            
            // Run fruit growth logic for all trees
            // UpdateFruit(ref tree);
            // Run tree growth logic
            if (!UpdateTree(ref tree))
            {
                continue;
            }
            
            if (tree.IsMature)
            {
                // Check for new fruit at regular intervals
                tree.TimeSinceLastFruitCheck += deltaTime;
                if (tree.TimeSinceLastFruitCheck >= 1.0f)
                {
                    tree.TimeSinceLastFruitCheck = 0;
                
                    // Check each potential fruit position
                    for (var i = 0; i < tree.AttachedFruits.Length; i++)
                    {
                        // If position is empty, consider creating a new fruit
                        if (tree.AttachedFruits[i] == new EntityId())
                        {
                            // Random chance to create a new fruit
                            if (_simulationWorld.Rng.RangeFloat(0, 1) < 0.1f) // 10% chance per second
                            {
                                var fruitEntityId = CreateFruit(tree, i);
                                tree.AttachedFruits[i] = fruitEntityId;
                            }
                        }
                    }
                }
            }
            
            
            
            

            _registry.UpdateComponent(tree);
        }
        Stepped?.Invoke();
    }

    private List<EntityId> treesThatShouldBeGone = new();
    
    public bool UpdateTree(ref TreeComponent tree)
    {
        // Store previous age to detect when we cross check thresholds
        var previousAge = tree.Age;
        
        // Update age
        tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
        
        // Check if we've crossed a death check interval threshold
        var shouldCheckDeath = (int)(previousAge / FruitTreeSimSettings.DeathCheckInterval) < 
                               (int)(tree.Age / FruitTreeSimSettings.DeathCheckInterval);
        
        if (!tree.IsMature)
        {
            // if (IsTooCloseToMatureTree(tree))
            // {
            //     tree.Alive = false;
            //     TreeDeathEvent?.Invoke(tree.EntityId);
            //     _registry.DestroyEntity(tree.EntityId);
            //     treesThatShouldBeGone.Add(tree.EntityId);
            //     return false;
            // }
            
            if (shouldCheckDeath)
            {
                var neighborCount = CountNeighbors(tree);
                var deathProbability = FruitTreeSimSettings.SaplingDeathProbabilityBase +
                                       neighborCount * FruitTreeSimSettings.SaplingDeathProbabilityPerNeighbor;

                if (_simulationWorld.Rng.rand.NextDouble() < deathProbability)
                {
                    tree.Alive = false;
                    TreeDeathEvent?.Invoke(tree.EntityId);
                    _registry.DestroyEntity(tree.EntityId);
                    treesThatShouldBeGone.Add(tree.EntityId);
                    return false;
                }
            }
        }
        else
        {
            tree.TimeSinceLastSpawn += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
            if (tree.TimeSinceLastSpawn >= FruitTreeSimSettings.TreeSpawnInterval)
            {
                tree.TimeSinceLastSpawn = 0;
            }

            // Only check mature tree death at intervals
            if (shouldCheckDeath)
            {
                var neighborCount = CountNeighbors(tree);
                var deathProbability = FruitTreeSimSettings.MatureTreeDeathProbabilityBase +
                                       neighborCount * FruitTreeSimSettings.MatureTreeDeathProbabilityPerNeighbor;
                if (_simulationWorld.Rng.rand.NextDouble() < deathProbability)
                {
                    tree.Alive = false;
                    TreeDeathEvent?.Invoke(tree.EntityId);
                    _registry.DestroyEntity(tree.EntityId);
                    treesThatShouldBeGone.Add(tree.EntityId);
                    return false;
                }
            }
        }

        return true;
    }
    #region Behaviors
    // private static readonly Shape3D MangoShape = 
    //     ResourceLoader.Load<Shape3D>("res://addons/PrimerTools/Simulation/Models/Mango/mango_shape_simplified.tres");
    private static readonly Shape3D MangoShape = 
        ResourceLoader.Load<Shape3D>("res://addons/PrimerTools/Simulation/Models/Mango/mango_shape.tres");
    public EntityId CreateFruit(TreeComponent tree, int positionIndex)
    {
        var entityId = _registry.CreateEntity();
        var fruitLocalPosition = FruitTreeSimSettings.StandardFruitPositions[positionIndex];
        var globalPosition = tree.Body.Transform.Origin + tree.Body.Transform.Basis * fruitLocalPosition;
        
        var fruitComponent = new FruitComponent(tree.EntityId, positionIndex)
        {
            EntityId = entityId,
            IsAttached = true
        };
        
        fruitComponent.Body = new BodyHandler(
            _simulationWorld.World3D.Space,
            new Transform3D(Basis.Identity, globalPosition),
            new Transform3D(
                Basis.FromEuler(new Vector3(-12f, 0, 5.7f) * Mathf.Pi / 180, EulerOrder.Xyz),
                new Vector3(0.012f, -0.444f, 0.006f)
            ),
            MangoShape
        );
        
        PhysicsServer3D.BodySetParam(fruitComponent.Body.Rid, PhysicsServer3D.BodyParameter.Mass, FruitTreeSimSettings.FruitMass);
        PhysicsServer3D.BodySetMode(fruitComponent.Body.Rid, PhysicsServer3D.BodyMode.Kinematic);
        CollisionRegistry.RegisterBody(fruitComponent.Body.Rid, typeof(FruitComponent), entityId);
        
        _registry.AddComponent(entityId, fruitComponent);
        return entityId;
    }

    private bool IsTooCloseToMatureTree(TreeComponent tree)
    {
        var nearbyEntities = CollisionDetector.GetEntitiesWithinRange(
            FruitTreeSimSettings.MinimumTreeDistance,
            tree.Body.Transform,
            SimulationWorld.Instance.World3D.Space,
            tree.Body.Rid
        );
        
        return nearbyEntities
            .Where(c => c.EntityType == typeof(TreeComponent))
            .Select(c => _registry.TryGetComponent<TreeComponent>(c.EntityId, out var component) ? component : default)
            .Any(t => t.IsMature);
    }

    private int CountNeighbors(TreeComponent tree)
    {
        var nearbyEntities = CollisionDetector.GetEntitiesWithinRange(
            FruitTreeSimSettings.TreeCompetitionRadius,
            tree.Body.Transform,
            SimulationWorld.Instance.World3D.Space,
            tree.Body.Rid
        );
        
        return nearbyEntities
            .Where(c => c.EntityType == typeof(TreeComponent))
            .Select(c => _registry.TryGetComponent<TreeComponent>(c.EntityId, out var component) ? component : default)
            .Count(t => t.Alive);
    }
    
    public Vector3 TryGenerateNewTreePosition(Vector3 position)
    {
        var angle = _simulationWorld.Rng.RangeFloat(0, Mathf.Tau);
        var distance = _simulationWorld.Rng.RangeFloat(FruitTreeSimSettings.MinTreeSpawnRadius, FruitTreeSimSettings.MaxTreeSpawnRadius);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
        return position + offset;
    }
    #endregion

    public void SaveTreeDistribution(string filePath = "")
    {
        if (string.IsNullOrEmpty(filePath))
        {
            // Get the project root path
            var projectPath = ProjectSettings.GlobalizePath("res://");
            var saveDir = Path.Combine(projectPath, "addons", "PrimerTools", "Simulation", "ContinuousSpaceTimeSims", "TreeSim", "Saved Tree Distributions");
            // Create directory if it doesn't exist
            Directory.CreateDirectory(saveDir);
            
            filePath = Path.Combine(saveDir, $"tree_distribution_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }

        var treesToSave = _registry.GetComponents<TreeComponent>()
            .Where(tree => tree.Alive)
            .Select(tree => new TreeDistributionData.TreeData(tree))
            .ToList();
        
        var distribution = new TreeDistributionData
        {
            Trees = treesToSave
        };
        
        var jsonString = JsonSerializer.Serialize(distribution);
        File.WriteAllText(filePath, jsonString);
    }

    private void LoadTreeDistribution()
    {
        if (_registry.GetComponents<TreeComponent>().Any())
        {
            GD.PrintErr("Cannot load tree distribution because there are already TreeComponents registered");
            return;
        } 
            
        var projectPath = ProjectSettings.GlobalizePath("res://");
        var path = Path.Combine(projectPath, FruitTreeSimSettings.TreeDistributionPath);
        if (!File.Exists(path))
        {
            GD.PrintErr($"Tree distribution file not found: {FruitTreeSimSettings.TreeDistributionPath}");
            return;
        }
        
        var jsonString = File.ReadAllText(path);
        
        var distribution = JsonSerializer.Deserialize<TreeDistributionData>(jsonString);
        
        if (distribution == null || distribution.Trees == null || !distribution.Trees.Any())
        {
            GD.PrintErr("Failed to deserialize tree distribution or no trees found");
            return;
        }
        
        foreach (var treeData in distribution.Trees)
        {
            var treeComponent = RegisterAndPlaceTreeEntity(new TreeComponent(), treeData.Transform);
            treeComponent.Age = treeData.Age;
            _registry.UpdateComponent(treeComponent);
        }
    }

    public IVisualEventManager CreateVisualEventManager(VisualEntityRegistry visualEntityRegistry)
    {
        return new TreeVisualEventManager(visualEntityRegistry);
    }
}
