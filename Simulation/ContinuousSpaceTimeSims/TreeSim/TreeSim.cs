using System;
using System.Diagnostics;
using Godot;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public partial class TreeSim : Node3D, ISimulation
{
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    [Export] public SimMode Mode = SimMode.TreeGrowth;
    
    public readonly TreeSimEntityRegistry Registry = new();
    private SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
    public VisualizationMode VisualizationMode => SimulationWorld.VisualizationMode;
    private int _stepsSoFar = 0;
    
    #region Editor controls
    private bool _running;
    [Export]
    public bool Running
    {
        get => _running;
        set
        {
            if (value && !_running && _stepsSoFar == 0)
            {
                Initialize();
                GD.Print("Starting tree sim.");
            }
            _running = value;
        }
    }
    
    #endregion
    
    #region Sim parameters
    [Export] private int _initialTreeCount = 20;
    
    private const float MaxTreeSpawnRadius = 5f;
    private const float MinTreeSpawnRadius = 1f;
    private const float TreeCompetitionRadius = 3f;
    private const float MinimumTreeDistance = 0.5f;
    
    private const float TreeMaturationTime = 1f;
    private const float TreeSpawnInterval = 0.4f;
    public const float FruitGrowthTime = 2f;
    public const float NodeFruitGrowthDelay = 0.5f;
    
    private const float SaplingDeathProbabilityBase = 0.001f;
    private const float SaplingDeathProbabilityPerNeighbor = 0.01f;
    private const float MatureTreeDeathProbabilityBase = 0.0001f;
    private const float MatureTreeDeathProbabilityPerNeighbor = 0.0001f;
    
    [Export] private float _deadTreeClearInterval = 1f;
    private float _timeSinceLastClear = 0f;
    #endregion
    
    // Performance testing
    private Stopwatch _stepStopwatch = new Stopwatch();
    private Stopwatch _processStopwatch = new Stopwatch();
    private Stopwatch _visualUpdateStopwatch = new Stopwatch();
    private double _totalStepTime;
    private double _totalProcessTime;
    private double _totalVisualUpdateTime;
    private int _stepCount;
    private int _processCount;
    
    private void Initialize()
    {
        Registry.World3D = SimulationWorld.World3D;

        for (var i = 0; i < _initialTreeCount; i++)
        {
            var position = new Vector3(
                SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                0,
                SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
            );
            Registry.CreateTree(position, this);
        }
    }

    public override void _Process(double delta)
    {
        if (!_running) return;
        
        if (SimulationWorld.PerformanceTest)
        {
            _processStopwatch.Restart();
            _visualUpdateStopwatch.Restart();
        }

        for (var i = 0; i < Registry.PhysicalTrees.Count; i++)
        {
            var physicalTree = Registry.PhysicalTrees[i]; 
            switch (VisualizationMode)
            {
                case VisualizationMode.None:
                    break;
                case VisualizationMode.Debug:
                    if (physicalTree.IsDead)
                    {
                        RenderingServer.InstanceSetVisible(Registry.VisualTrees[i].BodyMesh, false);
                        if (Registry.PhysicalTrees[i].HasFruit) RenderingServer.InstanceSetVisible(Registry.VisualTrees[i].FruitMesh, false);
                        continue;
                    }
                    if (!physicalTree.HasFruit && Registry.VisualTrees[i].FruitMesh != default)
                    {
                        RenderingServer.InstanceSetVisible(Registry.VisualTrees[i].FruitMesh, false);
                    }
                    if (physicalTree.HasFruit && Registry.VisualTrees[i].FruitMesh == default)
                    {
                        CreateFruitMesh(i, physicalTree.Position);
                    }
                    
                    if (physicalTree.IsMature)
                    {
                        var transform = Transform3D.Identity.Translated(physicalTree.Position);
                        transform = transform.ScaledLocal(Vector3.One * 1.0f);
                        RenderingServer.InstanceSetTransform(Registry.VisualTrees[i].BodyMesh, transform);
                    }
                    break;
                case VisualizationMode.NodeCreatures:
                    if (physicalTree.IsDead)
                    {
                        Registry.NodeTrees[i].Visible = false;
                    }
                    if (physicalTree.FruitGrowthProgress > NodeFruitGrowthDelay && !Registry.NodeTrees[i].HasFruit)
                    {
                        Registry.NodeTrees[i].GrowFruit(FruitGrowthTime - NodeFruitGrowthDelay);
                    }
                    if (physicalTree.IsMature)
                    {
                        Registry.NodeTrees[i].Scale = Vector3.One * 1.0f;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (SimulationWorld.PerformanceTest)
        {
            _visualUpdateStopwatch.Stop();
            _totalVisualUpdateTime += _visualUpdateStopwatch.Elapsed.TotalMilliseconds;
        }
        
        _timeSinceLastClear += (float)delta;
        if (_timeSinceLastClear >= _deadTreeClearInterval)
        {
            Registry.ClearDeadTrees();
            _timeSinceLastClear = 0f;
        }
        
        if (SimulationWorld.PerformanceTest)
        {
            _processStopwatch.Stop();
            _totalProcessTime += _processStopwatch.Elapsed.TotalMilliseconds;
            _processCount++;
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
        if (Registry.PhysicalTrees.Count == 0)
        {
            GD.Print("No Trees found. Stopping.");
            Running = false;
            return;
        }

        if (SimulationWorld.PerformanceTest)
        {
            _stepStopwatch.Restart();
        }

        var newTreePositions = new List<Vector3>();
        for (var i = 0; i < Registry.PhysicalTrees.Count; i++)
        {
            var tree = Registry.PhysicalTrees[i];
            if (tree.IsDead) continue;

            tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
            
            switch (Mode)
            {
                case SimMode.FruitGrowth:
                    if (tree.IsMature && !tree.HasFruit)
                    {
                        tree.FruitGrowthProgress += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
                        if (tree.FruitGrowthProgress >= FruitGrowthTime)
                        {
                            tree.HasFruit = true;
                        }
                    }
                    break;
                case SimMode.TreeGrowth:
                    if (!tree.IsMature)
                    {
                        var neighborCount = CountNeighbors(tree);
                        var deathProbability = SaplingDeathProbabilityBase +
                                               neighborCount * SaplingDeathProbabilityPerNeighbor;

                        // Check if sapling is too close to a mature tree
                        if (IsTooCloseToMatureTree(tree) || SimulationWorld.Rng.rand.NextDouble() < deathProbability)
                        {
                            tree.IsDead = true;
                        }

                        // Check for maturation
                        if (!tree.IsDead && tree.Age >= TreeMaturationTime)
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
                            TryGenerateNewTreePosition(tree, newTreePositions);
                        }

                        var neighborCount = CountNeighbors(tree);
                        var deathProbability = MatureTreeDeathProbabilityBase +
                                               neighborCount * MatureTreeDeathProbabilityPerNeighbor;
                        if (SimulationWorld.Rng.rand.NextDouble() < deathProbability)
                        {
                            tree.IsDead = true;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Registry.PhysicalTrees[i] = tree;
        }
        
        foreach (var newTreePosition in newTreePositions)
        {
            Registry.CreateTree(newTreePosition, this);
        }

        _stepsSoFar++;

        if (SimulationWorld.PerformanceTest)
        {
            _stepStopwatch.Stop();
            _totalStepTime += _stepStopwatch.Elapsed.TotalMilliseconds;
            _stepCount++;
        }
    }

    public void PrintPerformanceStats()
    {
        if (_stepCount > 0 && _processCount > 0)
        {
            GD.Print($"TreeSim Performance Stats:");
            GD.Print($"  Average Step Time: {_totalStepTime / _stepCount:F3} ms");
            GD.Print($"  Average Process Time: {_totalProcessTime / _processCount:F3} ms");
            GD.Print($"  Average Visual Update Time: {_totalVisualUpdateTime / _processCount:F3} ms");
        }
    }

    public void Reset()
    {
        _stepsSoFar = 0;
        Registry.Reset();
        
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
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