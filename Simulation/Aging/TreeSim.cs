using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using PrimerTools;
using PrimerTools.Simulation;
using PrimerTools.Simulation.Tree;
using PrimerTools.Simulation.Aging;

[Tool]
public partial class TreeSim : Node3D, ISimulation
{
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    [Export] private SimMode _mode = SimMode.TreeGrowth; 
    
    private SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
    
    #region Editor controls
    private bool _running;
    [Export]
    private bool Running
    {
        get => _running;
        set
        {
            if (value)
            {
                if (_stepsSoFar >= _maxNumSteps) Reset();
                if (_stepsSoFar == 0)
                {
                    Initialize();
                    GD.Print("Starting tree sim.");
                }
                else
                {
                    GD.Print($"Continuing tree sim after step {_stepsSoFar}");
                }
            }
            else if (_running)
            {
                GD.Print($"Stopping tree sim after step {_stepsSoFar}");
                if (_stopwatch != null)
                {
                    _stopwatch.Stop();
                    GD.Print($"Elapsed time: {_stopwatch.Elapsed}");
                }
            }
            
            _running = value;
        }
    }

    [Export] private bool _verbose;
    public bool Render { get; set; } = true;
    private Stopwatch _stopwatch;
    #endregion
    
    #region Sim parameters
    [Export] private int _initialTreeCount = 5;
    [Export] private int _maxNumSteps = 1000;
    [Export] private float _deadTreeClearInterval = 1f;
    private float _timeSinceLastClear = 0f;
    private const float TreeMaturationTime = 1f;
    private const float TreeSpawnInterval = 0.4f;
    private const float MaxTreeSpawnRadius = 5f;
    private const float MinTreeSpawnRadius = 1f;
    private const float TreeCompetitionRadius = 3f;
    private const float MinimumTreeDistance = 0.5f;
    private const float SaplingDeathProbabilityBase = 0.001f;
    private const float SaplingDeathProbabilityPerNeighbor = 0.01f;
    private const float MatureTreeDeathProbabilityBase = 0.0001f;
    private const float MatureTreeDeathProbabilityPerNeighbor = 0.0001f;
    private const float FruitGrowthTime = 5f;
    private int _stepsSoFar = 0;
    #endregion
    
    public TreeSimEntityRegistry Registry = new();

    private void Initialize()
    {
        
        Registry.World3D = SimulationWorld.World3D;
        _stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < _initialTreeCount; i++)
        {
            Registry.CreateTree(
                new Vector3(
                    SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                    0,
                    SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
                ),
                Render
            );
        }
    }

    public override void _Process(double delta)
    {
        if (!_running) return;

        _timeSinceLastClear += (float)delta;
        if (_timeSinceLastClear >= _deadTreeClearInterval)
        {
            Registry.ClearDeadTrees(Render);
            _timeSinceLastClear = 0f;
        }
    }

    private void TryGenerateNewTreePosition(TreeSimEntityRegistry.PhysicalTree parent, List<Vector3> newTrees)
    {
        var angle = SimulationWorld.Rng.RangeFloat(0, Mathf.Tau);
        var distance = SimulationWorld.Rng.RangeFloat(MinTreeSpawnRadius, MaxTreeSpawnRadius);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
        var newPosition = parent.Position + offset;

        if (IsWithinWorldBounds(newPosition))
        {
            newTrees.Add(newPosition);
        }
    }

    private int CountNeighbors(TreeSimEntityRegistry.PhysicalTree tree)
    {
        var queryParams = new PhysicsShapeQueryParameters3D();
        queryParams.CollideWithAreas = true;
        queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(tree.Body, 0);
        var transform = Transform3D.Identity.Translated(tree.Position);
        transform = transform.ScaledLocal(Vector3.One * TreeCompetitionRadius);
        queryParams.Transform = transform;

        var intersections = PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space).IntersectShape(queryParams);
        int livingNeighbors = 0;

        foreach (var intersection in intersections)
        {
            var intersectedBody = (Rid)intersection["rid"];
            if (Registry.TreeLookup.TryGetValue(intersectedBody, out int index))
            {
                if (!Registry.PhysicalTrees[index].IsDead && intersectedBody != tree.Body)
                {
                    livingNeighbors++;
                }
            }
        }

        return livingNeighbors;
    }

    private bool IsTooCloseToMatureTree(TreeSimEntityRegistry.PhysicalTree sapling)
    {
        var queryParams = new PhysicsShapeQueryParameters3D();
        queryParams.CollideWithAreas = true;
        queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(sapling.Body, 0);
        var transform = Transform3D.Identity.Translated(sapling.Position);
        transform = transform.ScaledLocal(Vector3.One * MinimumTreeDistance);
        queryParams.Transform = transform;

        var intersections = PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space).IntersectShape(queryParams);
        
        foreach (var intersection in intersections)
        {
            var intersectedBody = (Rid)intersection["rid"];
            if (Registry.TreeLookup.TryGetValue(intersectedBody, out int index))
            {
                if (Registry.PhysicalTrees[index].IsMature)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private bool IsWithinWorldBounds(Vector3 position)
    {
        return SimulationWorld.IsWithinWorldBounds(position);
    }

    public void Step()
    {
        if (!_running) return;
        if (_stepsSoFar >= _maxNumSteps)
        {
            GD.Print("Done");
            Running = false;
            return;
        }
        if (Registry.PhysicalTrees.Count == 0)
        {
            GD.Print("No Trees found. Stopping.");
            Running = false;
            return;
        }

        for (var i = 0; i < Registry.PhysicalTrees.Count; i++)
        {
            var tree = Registry.PhysicalTrees[i];
            if (tree.IsDead) continue;

            tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;

            switch (_mode)
            {
                case SimMode.FruitGrowth:
                    if (tree.IsMature && !tree.HasFruit)
                    {
                        tree.FruitGrowthProgress += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
                        if (tree.FruitGrowthProgress >= FruitGrowthTime)
                        {
                            tree.HasFruit = true;
                            if (Render)
                            {
                                CreateFruitMesh(i, tree.Position);
                            }
                        }
                    }
                    break;
                case SimMode.TreeGrowth:
                    var newTreePositions = new List<Vector3>();
                    if (!tree.IsMature)
                    {
                        var neighborCount = CountNeighbors(tree);
                        var deathProbability = SaplingDeathProbabilityBase +
                                               neighborCount * SaplingDeathProbabilityPerNeighbor;

                        // Check if sapling is too close to a mature tree
                        if (IsTooCloseToMatureTree(tree) || SimulationWorld.Rng.rand.NextDouble() < deathProbability)
                        {
                            tree.IsDead = true;
                            RenderingServer.InstanceSetVisible(Registry.VisualTrees[i].BodyMesh, false);
                        }

                        // Check for maturation
                        if (!tree.IsDead && tree.Age >= TreeMaturationTime)
                        {
                            tree.IsMature = true;
                            var transform = Transform3D.Identity.Translated(tree.Position);
                            transform = transform.ScaledLocal(Vector3.One * 1.0f);
                            RenderingServer.InstanceSetTransform(Registry.VisualTrees[i].BodyMesh, transform);
                        }
                    }
                    else
                    {
                        tree.TimeSinceLastSpawn += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
                        if (tree.TimeSinceLastSpawn >= TreeSpawnInterval)
                        {
                            tree.TimeSinceLastSpawn = 0;
                            TryGenerateNewTreePosition(tree, newTreePositions);
                        }

                        var neighborCount = CountNeighbors(tree);
                        var deathProbability = MatureTreeDeathProbabilityBase +
                                               neighborCount * MatureTreeDeathProbabilityPerNeighbor;
                        if (SimulationWorld.Rng.rand.NextDouble() < deathProbability)
                        {
                            tree.IsDead = true;
                            RenderingServer.InstanceSetVisible(Registry.VisualTrees[i].BodyMesh, false);
                        }
                    }
                    foreach (var newTreePosition in newTreePositions)
                    {
                        Registry.CreateTree(newTreePosition, Render);
                    }
                    break;
            }

            Registry.PhysicalTrees[i] = tree;
        }

        _stepsSoFar++;
    }

    public void Reset()
    {
        _stepsSoFar = 0;
        Registry.Reset();
    }

    private void CreateFruitMesh(int treeIndex, Vector3 treePosition)
    {
        var fruitMesh = new SphereMesh();
        fruitMesh.Radius = 0.6f;
        fruitMesh.Height = 1.2f;

        var fruitMaterial = new StandardMaterial3D();
        fruitMaterial.AlbedoColor = new Color(0, 1, 0); // Red color for the fruit
        fruitMesh.Material = fruitMaterial;

        var fruitInstance = RenderingServer.InstanceCreate2(fruitMesh.GetRid(), GetWorld3D().Scenario);
        var fruitTransform = Transform3D.Identity.Translated(treePosition + new Vector3(0, 1.5f, 0)); // Position the fruit above the tree
        RenderingServer.InstanceSetTransform(fruitInstance, fruitTransform);

        var visualTree = Registry.VisualTrees[treeIndex];
        visualTree.FruitMesh = fruitInstance;
        visualTree.FruitMeshResource = fruitMesh;
        Registry.VisualTrees[treeIndex] = visualTree;
    }
}
