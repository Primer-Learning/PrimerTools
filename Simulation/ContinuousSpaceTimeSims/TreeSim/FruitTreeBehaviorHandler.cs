using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation
{
    public static class FruitTreeBehaviorHandler
    {
        #region Simulation Parameters
        public const float MaxTreeSpawnRadius = 5f;
        public const float MinTreeSpawnRadius = 1f;
        public const float TreeCompetitionRadius = 3f;
        public const float MinimumTreeDistance = 0.5f;
    
        public static float TreeMaturationTime = 1f;
        public const float TreeSpawnInterval = 0.4f;
        public static float FruitGrowthTime = 4f;
        public const float NodeFruitGrowthDelay = 2f;
    
        private const float SaplingDeathProbabilityBase = 0.001f;
        private const float SaplingDeathProbabilityPerNeighbor = 0.01f;
        private const float MatureTreeDeathProbabilityBase = 0.0001f;
        private const float MatureTreeDeathProbabilityPerNeighbor = 0.0001f;
        #endregion

        #region Behaviors
        public static void UpdateTree(ref DataTree tree, PhysicsDirectSpaceState3D spaceState, DataTreeRegistry registry)
        {
            tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
            if (!tree.IsMature)
            {
                var neighborCount = CountNeighbors(tree, spaceState, registry);
                var deathProbability = SaplingDeathProbabilityBase +
                                       neighborCount * SaplingDeathProbabilityPerNeighbor;

                // Check if sapling is too close to a mature tree
                if (IsTooCloseToMatureTree(tree, spaceState, registry) || SimulationWorld.Rng.rand.NextDouble() < deathProbability)
                {
                    tree.Alive = false;
                }

                // Check for maturation
                if (tree.Alive && tree.Age >= TreeMaturationTime)
                {
                    tree.IsMature = true;
                }
            }
            else
            {
                tree.TimeSinceLastSpawn += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
                if (tree.TimeSinceLastSpawn >= TreeSpawnInterval)
                {
                    tree.TimeSinceLastSpawn = 0;
                    // Note: TryGenerateNewTreePosition is not implemented here as it requires access to the list of new trees
                }

                var neighborCount = CountNeighbors(tree, spaceState, registry);
                var deathProbability = MatureTreeDeathProbabilityBase +
                                       neighborCount * MatureTreeDeathProbabilityPerNeighbor;
                if (SimulationWorld.Rng.rand.NextDouble() < deathProbability)
                {
                    tree.Alive = false;
                }
            }
        }

        public static void UpdateFruit(ref DataTree tree)
        {
            if (tree.IsMature && !tree.HasFruit)
            {
                tree.FruitGrowthProgress += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
                if (tree.FruitGrowthProgress >= FruitGrowthTime)
                {
                    tree.HasFruit = true;
                }
            }
        }

        private static bool IsTooCloseToMatureTree(DataTree sapling, PhysicsDirectSpaceState3D spaceState, DataTreeRegistry registry)
        {
            var queryParams = new PhysicsShapeQueryParameters3D();
            queryParams.CollideWithAreas = true;
            queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(sapling.Body, 0);
            var transform = Transform3D.Identity.Translated(sapling.Position);
            transform = transform.ScaledLocal(Vector3.One * MinimumTreeDistance);
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

        private static int CountNeighbors(DataTree tree, PhysicsDirectSpaceState3D spaceState, DataTreeRegistry registry)
        {
            var queryParams = new PhysicsShapeQueryParameters3D();
            queryParams.CollideWithAreas = true;
            queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(tree.Body, 0);
            var transform = Transform3D.Identity.Translated(tree.Position);
            transform = transform.ScaledLocal(Vector3.One * TreeCompetitionRadius);
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
        
        public static Vector3 TryGenerateNewTreePosition(DataTree parent)
        {
            var angle = SimulationWorld.Rng.RangeFloat(0, Mathf.Tau);
            var distance = SimulationWorld.Rng.RangeFloat(MinTreeSpawnRadius, MaxTreeSpawnRadius);
            var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
            return parent.Position + offset;
        }
        #endregion
    }
}