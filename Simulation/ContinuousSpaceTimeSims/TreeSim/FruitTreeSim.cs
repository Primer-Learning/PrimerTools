using System;
using System.Diagnostics;
using Godot;
using System.Collections.Generic;
using System.Linq;
using PrimerTools;
using PrimerTools.Simulation;
using PrimerTools.Simulation.TreeSim;

[Tool]
public partial class FruitTreeSim : Node3D, ISimulation
{
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    [Export] public SimMode Mode = SimMode.TreeGrowth;

    public PhysicalTreeRegistry Registry;
    public IEntityRegistry<IVisualTree> VisualTreeRegistry;
    private SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
    public VisualizationMode VisualizationMode => SimulationWorld.VisualizationMode;
    private int _stepsSoFar;
    
    #region Editor controls
    private bool _running;
    public bool Running
    {
        get => _running;
        set
        {
            if (value && !_initialized)
            {
                Initialize();
                GD.Print("Starting tree sim.");
            }
            _running = value;
            GD.Print(_initialized);
        }
    }
    #endregion
    
    #region Sim parameters
    [Export] private int _initialTreeCount = 20;
    
    private const float MaxTreeSpawnRadius = 5f;
    private const float MinTreeSpawnRadius = 1f;
    private const float TreeCompetitionRadius = 3f;
    private const float MinimumTreeDistance = 0.5f;
    
    public static float TreeMaturationTime = 1f;
    private const float TreeSpawnInterval = 0.4f;
    public const float FruitGrowthTime = 4f;
    public const float NodeFruitGrowthDelay = 2f;
    
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
    
    private bool _initialized;
    public void Initialize()
    {
        Registry = new PhysicalTreeRegistry(SimulationWorld.World3D);
        
        switch (SimulationWorld.VisualizationMode)
        {
            case VisualizationMode.None:
                break;
            case VisualizationMode.Debug:
                VisualTreeRegistry = new VisualDebugTreeRegistry(SimulationWorld.World3D);
                break;
            case VisualizationMode.NodeCreatures:
                VisualTreeRegistry = new NodeTreeRegistry(this);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        for (var i = 0; i < _initialTreeCount; i++)
        {
            var physicalTree = new PhysicalTree();
            physicalTree.Position = new Vector3(
                SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                0,
                SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
            );
            RegisterTree(physicalTree);
        }

        _initialized = true;
    }

    private void RegisterTree(PhysicalTree physicalTree)
    {
        Registry.RegisterEntity(physicalTree);
        VisualTreeRegistry.RegisterEntity(physicalTree);
    }

    public override void _Process(double delta)
    {
        if (!_running) return;
        
        if (SimulationWorld.PerformanceTest)
        {
            _processStopwatch.Restart();
            _visualUpdateStopwatch.Restart();
        }

        for (var i = 0; i < Registry.Entities.Count; i++)
        {
            var physicalTree = Registry.Entities[i]; 
            var visualTree = VisualTreeRegistry.Entities[i];
            
            if (physicalTree.IsDead)
            {
                visualTree.Death();
                VisualTreeRegistry.Entities[i] = visualTree;
                continue;
            }
            
            if (physicalTree.FruitGrowthProgress > NodeFruitGrowthDelay && !visualTree.HasFruit)
            {
                visualTree.GrowFruit(FruitGrowthTime - NodeFruitGrowthDelay);
            }
            visualTree.UpdateTransform(physicalTree);

            VisualTreeRegistry.Entities[i] = visualTree;
        }

        if (SimulationWorld.PerformanceTest)
        {
            _visualUpdateStopwatch.Stop();
            _totalVisualUpdateTime += _visualUpdateStopwatch.Elapsed.TotalMilliseconds;
        }
        
        _timeSinceLastClear += (float)delta;
        if (_timeSinceLastClear >= _deadTreeClearInterval)
        {
            ClearDeadTrees();
            _timeSinceLastClear = 0f;
        }
        
        if (SimulationWorld.PerformanceTest)
        {
            _processStopwatch.Stop();
            _totalProcessTime += _processStopwatch.Elapsed.TotalMilliseconds;
            _processCount++;
        }
    }

    private void TryGenerateNewTreePosition(PhysicalTree parent, List<Vector3> newTrees)
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

    private int CountNeighbors(PhysicalTree tree)
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
            if (Registry.TreeLookup.TryGetValue(intersectedBody, out var index))
            {
                var dataTree = Registry.Entities[index];
                if (!dataTree.IsDead && intersectedBody != tree.Body)
                {
                    livingNeighbors++;
                }
            }
        }

        return livingNeighbors;
    }

    private bool IsTooCloseToMatureTree(PhysicalTree sapling)
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
            if (Registry.TreeLookup.TryGetValue(intersectedBody, out var index))
            {
                if (Registry.Entities[index].IsMature)
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
        if (Registry.Entities.Count == 0)
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
        for (var i = 0; i < Registry.Entities.Count; i++)
        {
            var tree = Registry.Entities[i];
            if (tree.IsDead) continue;
            
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
                    tree.Age += 1f / SimulationWorld.PhysicsStepsPerSimSecond;
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

            Registry.Entities[i] = tree;
        }
        
        foreach (var newTreePosition in newTreePositions)
        {
            var physicalTree = new PhysicalTree();
            physicalTree.Position = newTreePosition;
            RegisterTree(physicalTree);
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
        _initialized = false;
        Registry?.Reset();
        
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
    }
    
    public void ClearDeadTrees()
    {
        // TODO: Probably best to make the registries take care of this. They do need to be triggered together, though.
        
        for (var i = Registry.Entities.Count - 1; i >= 0; i--)
        {
            if (!Registry.Entities[i].IsDead) continue;
			     
            Registry.Entities[i].CleanUp();
            Registry.Entities.RemoveAt(i);
        
            if (VisualTreeRegistry.Entities.Count > 0)
            {
                // Visual trees aren't cleaned up here, since they may want to do an animation before freeing the object
                // But we clear the list here so they stay in sync.
                // For this reason, _creatureVisualizer.CreatureDeath must handle cleanup.
                VisualTreeRegistry.Entities.RemoveAt(i);
            }
        }
        
        // Rebuild TreeLookup
        Registry.TreeLookup.Clear();
        for (int i = 0; i < Registry.Entities.Count; i++)
        {
            Registry.TreeLookup[Registry.Entities[i].Body] = i;
        }
    }
}
