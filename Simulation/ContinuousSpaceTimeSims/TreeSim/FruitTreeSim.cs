using System;
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public class FruitTreeSim : Simulation<DataTree>
{
    private FruitTreeSimSettings _settings;

    public FruitTreeSim(SimulationWorld simulationWorld, FruitTreeSimSettings settings) : base(simulationWorld)
    {
        _settings = settings;
    }
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    public SimMode Mode = SimMode.TreeGrowth;

    protected override void CustomInitialize(IEnumerable<Vector3> initialPositions)
    {
        GD.Print("initialize trees");
        if (_settings.LoadTreeDistribution)
        {
            LoadTreeDistribution();
            return;
        }

        if (initialPositions == null)
        {
            var posList = new List<Vector3>();
            for (var i = 0; i < InitialEntityCount; i++)
            {
                posList.Add(
                    new Vector3(
                        SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                        0,
                        SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
                    )
                );
            }

            initialPositions = posList;
        }

        foreach (var pos in initialPositions)
        {
            Registry.RegisterEntity(new DataTree { Position = pos });
        }
    }
    protected override void CustomStep()
    {
        var newTreePositions = new List<Vector3>();
        for (var i = 0; i < Registry.Entities.Count; i++)
        {
            var tree = Registry.Entities[i];
            if (!tree.Alive) continue;
            
            switch (Mode)
            {
                case SimMode.FruitGrowth:
                    UpdateFruit(ref tree);
                    break;
                case SimMode.TreeGrowth:
                    UpdateTree(ref tree, PhysicsServer3D.SpaceGetDirectState(SimulationWorld.GetWorld3D().Space), Registry);
                    if (tree is { IsMature: true, TimeSinceLastSpawn: 0 })
                    {
                        var newPosition = TryGenerateNewTreePosition(tree);
                        if (SimulationWorld.IsWithinWorldBounds(newPosition))
                        {
                            newTreePositions.Add(newPosition);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Registry.Entities[i] = tree;
        }
        foreach (var newTreePosition in newTreePositions)
            
        {
            var physicalTree = new DataTree
            {
                Position = newTreePosition
            };
            Registry.RegisterEntity(physicalTree);
        }
    }
    
    public void UpdateTree(ref DataTree tree, PhysicsDirectSpaceState3D spaceState, DataEntityRegistry<DataTree> registry)
    {
        tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
        if (!tree.IsMature)
        {
            var neighborCount = CountNeighbors(tree, spaceState, registry);
            var deathProbability = FruitTreeSimSettings.SaplingDeathProbabilityBase +
                                   neighborCount * FruitTreeSimSettings.SaplingDeathProbabilityPerNeighbor;

            // Check if sapling is too close to a mature tree
            if (IsTooCloseToMatureTree(tree, spaceState, registry) || SimulationWorld.Rng.rand.NextDouble() < deathProbability)
            {
                tree.Alive = false;
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

            var neighborCount = CountNeighbors(tree, spaceState, registry);
            var deathProbability = FruitTreeSimSettings.MatureTreeDeathProbabilityBase +
                                   neighborCount * FruitTreeSimSettings.MatureTreeDeathProbabilityPerNeighbor;
            if (SimulationWorld.Rng.rand.NextDouble() < deathProbability)
            {
                tree.Alive = false;
            }
        }
    }
    #region Behaviors

    public static void UpdateFruit(ref DataTree tree)
    {
        if (tree.IsMature && !tree.HasFruit)
        {
            tree.FruitGrowthProgress += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
            if (tree.FruitGrowthProgress >= FruitTreeSimSettings.FruitGrowthTime)
            {
                tree.HasFruit = true;
            }
        }
    }

    private static bool IsTooCloseToMatureTree(DataTree sapling, PhysicsDirectSpaceState3D spaceState, DataEntityRegistry<DataTree> registry)
    {
        var queryParams = new PhysicsShapeQueryParameters3D();
        queryParams.CollideWithAreas = true;
        queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(sapling.Body, 0);
        var transform = Transform3D.Identity.Translated(sapling.Position);
        transform = transform.ScaledLocal(Vector3.One * FruitTreeSimSettings.MinimumTreeDistance);
        queryParams.Transform = transform;

        var intersections = spaceState.IntersectShape(queryParams);
    
        foreach (var intersection in intersections)
        {
            var intersectedBody = (Rid)intersection["rid"];
            if (registry.EntityLookup.TryGetValue(intersectedBody, out var index))
            {
                if (registry.Entities[index].IsMature)
                {
                    return true;
                }
            }
        }
    
        return false;
    }

    private static int CountNeighbors(DataTree tree, PhysicsDirectSpaceState3D spaceState, DataEntityRegistry<DataTree> registry)
    {
        var queryParams = new PhysicsShapeQueryParameters3D();
        queryParams.CollideWithAreas = true;
        queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(tree.Body, 0);
        var transform = Transform3D.Identity.Translated(tree.Position);
        transform = transform.ScaledLocal(Vector3.One * FruitTreeSimSettings.TreeCompetitionRadius);
        queryParams.Transform = transform;

        var intersections = spaceState.IntersectShape(queryParams);
        int livingNeighbors = 0;

        foreach (var intersection in intersections)
        {
            var intersectedBody = (Rid)intersection["rid"];
            if (registry.EntityLookup.TryGetValue(intersectedBody, out var index))
            {
                var dataTree = registry.Entities[index];
                if (dataTree.Alive && intersectedBody != tree.Body)
                {
                    livingNeighbors++;
                }
            }
        }

        return livingNeighbors;
    }
    
    public Vector3 TryGenerateNewTreePosition(DataTree parent)
    {
        var angle = SimulationWorld.Rng.RangeFloat(0, Mathf.Tau);
        var distance = SimulationWorld.Rng.RangeFloat(FruitTreeSimSettings.MinTreeSpawnRadius, FruitTreeSimSettings.MaxTreeSpawnRadius);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
        return parent.Position + offset;
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

        GD.Print(SimulationWorld.WorldDimensions);
        var treesToSave = Registry.Entities
            .Where(tree => tree.Alive)
            .Select(tree => new TreeDistributionData.TreeData(tree))
            .ToList();
        
        var distribution = new TreeDistributionData
        {
            // WorldDimensions = simulationWorld.WorldDimensions,
            Trees = treesToSave
        };
        
        var jsonString = JsonSerializer.Serialize(distribution);
        File.WriteAllText(filePath, jsonString);
    }

    private void LoadTreeDistribution()
    {
        var projectPath = ProjectSettings.GlobalizePath("res://");
        var path = Path.Combine(projectPath, _settings.TreeDistributionPath);
        if (!File.Exists(path))
        {
            GD.PrintErr($"Tree distribution file not found: {_settings.TreeDistributionPath}");
            return;
        }
        
        var jsonString = File.ReadAllText(path);
        
        var distribution = JsonSerializer.Deserialize<TreeDistributionData>(jsonString);
        
        if (distribution == null || distribution.Trees == null || !distribution.Trees.Any())
        {
            GD.PrintErr("Failed to deserialize tree distribution or no trees found");
            return;
        }

        Registry.Reset();
        
        foreach (var treeData in distribution.Trees)
        {
            var tree = new DataTree
            {
                Position = treeData.Position,
                Age = treeData.Age,
            };
            Registry.RegisterEntity(tree);
        }
    }
}
