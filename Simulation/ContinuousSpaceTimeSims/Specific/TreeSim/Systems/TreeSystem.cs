using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using GladiatorManager.ContinuousSpaceTimeSims.CreatureSim.Visual;
using Godot;
using Microsoft.Win32;
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
                Age = FruitTreeSimSettings.TreeMaturationTime
            };
            RegisterAndPlaceTreeEntity(newTree, pos);
        }
    }

    private TreeComponent RegisterAndPlaceTreeEntity(TreeComponent treeComponent, Vector3 position)
    {
        var entityId = _registry.CreateEntity();
        
        // Ground position. Just make y zero for now. Eventually probably raycast to find the ground.
        var groundedPosition = new Vector3(
            position.X,
            0,
            position.Z
        );
        
        // Physics first so it exists when the VisualSystem gets the event
        var physicsComponent = new AreaPhysicsComponent(
            _simulationWorld.World3D.Space,
            groundedPosition,
            new SphereShape3D { Radius = 1.0f }
        );
        _registry.AddComponent(entityId, physicsComponent);
        CollisionRegistry.RegisterBody(physicsComponent.GetBodyRid(), typeof(TreeComponent), entityId);

        treeComponent.Angle = _simulationWorld.Rng.RangeFloat(0, Mathf.Tau);
        _registry.AddComponent(entityId, treeComponent);

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
            UpdateFruit(ref tree);
            
            // Run tree growth logic
            var physicsComponent = _registry.GetComponent<AreaPhysicsComponent>(tree.EntityId);
            if (!UpdateTree(ref tree, ref physicsComponent, _simulationWorld.GetWorld3D().Space))
            {
                continue;
            }
            
            // Handle tree reproduction
            if (tree is { IsMature: true, TimeSinceLastSpawn: 0 })
            {
                var newPosition = TryGenerateNewTreePosition(physicsComponent);
                if (_simulationWorld.IsWithinWorldBounds(newPosition))
                {
                    newTreePositions.Add(newPosition);
                }
            }

            _registry.UpdateComponent(tree);
        }
        foreach (var newTreePosition in newTreePositions)
        {
            RegisterAndPlaceTreeEntity(new TreeComponent(), newTreePosition);
        }
        Stepped?.Invoke();
    }

    private List<EntityId> treesThatShouldBeGone = new();
    
    public bool UpdateTree(ref TreeComponent tree, ref AreaPhysicsComponent areaPhysicsComponent, Rid space)
    {
        tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
        if (!tree.IsMature)
        {
            var neighborCount = CountNeighbors(areaPhysicsComponent, space);
            var deathProbability = FruitTreeSimSettings.SaplingDeathProbabilityBase +
                                   neighborCount * FruitTreeSimSettings.SaplingDeathProbabilityPerNeighbor;

            // Check if sapling is too close to a mature tree
            if (IsTooCloseToMatureTree(areaPhysicsComponent, space) || _simulationWorld.Rng.rand.NextDouble() < deathProbability)
            {
                tree.Alive = false;
                TreeDeathEvent?.Invoke(tree.EntityId);
                _registry.DestroyEntity(tree.EntityId);
                treesThatShouldBeGone.Add(tree.EntityId);
                // GD.Print($"Called DestroyEntity on {tree.EntityId.Value}");
                return false;
            }
        }
        else
        {
            tree.TimeSinceLastSpawn += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
            if (tree.TimeSinceLastSpawn >= FruitTreeSimSettings.TreeSpawnInterval)
            {
                tree.TimeSinceLastSpawn = 0;
                // Note: TryGenerateNewTreePosition is not implemented here as it requires access to the list of new trees
            }

            var neighborCount = CountNeighbors(areaPhysicsComponent, space);
            var deathProbability = FruitTreeSimSettings.MatureTreeDeathProbabilityBase +
                                   neighborCount * FruitTreeSimSettings.MatureTreeDeathProbabilityPerNeighbor;
            if (_simulationWorld.Rng.rand.NextDouble() < deathProbability)
            {
                tree.Alive = false;
                TreeDeathEvent?.Invoke(tree.EntityId);
                _registry.DestroyEntity(tree.EntityId);
                treesThatShouldBeGone.Add(tree.EntityId);
                // GD.Print($"Called DestroyEntity on {tree.EntityId.Value}");
                return false;
            }
        }

        return true;
    }
    #region Behaviors

    public static void UpdateFruit(ref TreeComponent tree)
    {
        if (tree.IsMature && !tree.HasFruit)
        {
            tree.FruitGrowthProgress += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
            if (tree.FruitGrowthProgress >= FruitTreeSimSettings.FruitGrowthTime)
            {
                tree.HasFruit = true;
            }
        }
        // Note: Future enhancement could include fruit lifecycle (falling, decay, etc.)
    }

    private bool IsTooCloseToMatureTree(AreaPhysicsComponent areaPhysicsComponent, Rid space)
    {
        var transform = Transform3D.Identity.Translated(areaPhysicsComponent.Position);
        transform = transform.ScaledLocal(Vector3.One * FruitTreeSimSettings.MinimumTreeDistance);

        var nearbyEntities = CollisionDetector.GetOverlappingEntitiesWithArea(
            areaPhysicsComponent.Body.Area,
            transform,
            space
        );
        
        return nearbyEntities
            .Where(c => c.EntityType == typeof(TreeComponent))
            .Select(c => _registry.TryGetComponent<TreeComponent>(c.EntityId, out var component) ? component : default)
            .Any(t => t.IsMature);
    }

    private int CountNeighbors(AreaPhysicsComponent areaPhysicsComponent, Rid space)
    {
        var transform = Transform3D.Identity.Translated(areaPhysicsComponent.Position);
        transform = transform.ScaledLocal(Vector3.One * FruitTreeSimSettings.TreeCompetitionRadius);
        
        var nearbyEntities = CollisionDetector.GetOverlappingEntitiesWithArea(
            areaPhysicsComponent.Body.Area,
            transform,
            space
        );
        
        return nearbyEntities
            .Where(c => c.EntityType == typeof(TreeComponent))
            .Select(c => _registry.TryGetComponent<TreeComponent>(c.EntityId, out var component) ? component : default)
            .Count(t => t.Alive);
    }
    
    public Vector3 TryGenerateNewTreePosition(AreaPhysicsComponent areaPhysicsComponent)
    {
        var angle = _simulationWorld.Rng.RangeFloat(0, Mathf.Tau);
        var distance = _simulationWorld.Rng.RangeFloat(FruitTreeSimSettings.MinTreeSpawnRadius, FruitTreeSimSettings.MaxTreeSpawnRadius);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
        return areaPhysicsComponent.Position + offset;
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
            .Select(tree => new TreeDistributionData.TreeData(tree, _registry.GetComponent<AreaPhysicsComponent>(tree.EntityId)))
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
            var treeComponent = RegisterAndPlaceTreeEntity(new TreeComponent(), treeData.Position);
            treeComponent.Angle = treeData.Angle;
            treeComponent.Age = treeData.Age;
            _registry.UpdateComponent(treeComponent);
        }
    }

    public IVisualEventManager CreateVisualEventManager(VisualEntityRegistry visualEntityRegistry)
    {
        return new TreeVisualEventManager(visualEntityRegistry);
    }
}
