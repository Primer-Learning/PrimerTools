using System;
using Godot;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public class FruitTreeSim : Simulation<DataTree>
{
    public FruitTreeSim(SimulationWorld simulationWorld) : base(simulationWorld) {}
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    public SimMode Mode = SimMode.TreeGrowth;

    protected override void CustomInitialize()
    {
        for (var i = 0; i < InitialEntityCount; i++)
        {
            var physicalTree = new DataTree
            {
                Position = new Vector3(
                    simulationWorld.Rng.RangeFloat(simulationWorld.WorldDimensions.X),
                    0,
                    simulationWorld.Rng.RangeFloat(simulationWorld.WorldDimensions.Y)
                )
            };
            Registry.RegisterEntity(physicalTree);
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
                    UpdateTree(ref tree, PhysicsServer3D.SpaceGetDirectState(simulationWorld.GetWorld3D().Space), Registry);
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
            if (IsTooCloseToMatureTree(tree, spaceState, registry) || simulationWorld.Rng.rand.NextDouble() < deathProbability)
            {
                tree.Alive = false;
            }

            // Check for maturation
            if (tree.Alive && tree.Age >= FruitTreeSimSettings.TreeMaturationTime)
            {
                tree.IsMature = true;
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
            if (simulationWorld.Rng.rand.NextDouble() < deathProbability)
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
            var angle = simulationWorld.Rng.RangeFloat(0, Mathf.Tau);
            var distance = simulationWorld.Rng.RangeFloat(FruitTreeSimSettings.MinTreeSpawnRadius, FruitTreeSimSettings.MaxTreeSpawnRadius);
            var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
            return parent.Position + offset;
        }
        #endregion
}
