using System;
using System.Diagnostics;
using Godot;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public partial class FruitTreeSim : Simulation
{
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
    
    [Export] private float _deadTreeClearInterval = 1f;
    private float _timeSinceLastClear = 0f;
    
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    [Export] public SimMode Mode = SimMode.TreeGrowth;
    #endregion

    #region Simulation
    private SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
    public DataTreeRegistry Registry;
    public IEntityRegistry<NodeTree> VisualTreeRegistry;
    private int _stepsSoFar;

    #region Life cycle
    private bool _initialized;
    public override void Initialize()
    {
        Registry = new DataTreeRegistry(SimulationWorld.World3D);
        
        switch (SimulationWorld.VisualizationMode)
        {
            case VisualizationMode.None:
                break;
            case VisualizationMode.NodeCreatures:
                VisualTreeRegistry = new NodeTreeRegistry(this);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        for (var i = 0; i < _initialTreeCount; i++)
        {
            var physicalTree = new DataTree();
            physicalTree.Position = new Vector3(
                SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                0,
                SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
            );
            RegisterTree(physicalTree);
        }

        _initialized = true;
    }
    public override void Reset()
    {
        _stepsSoFar = 0;
        _initialized = false;
        Registry?.Reset();
        
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
    }
    #endregion
    public override void Step()
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
                    FruitTreeBehaviorHandler.UpdateFruit(ref tree, SimulationWorld);
                    break;
                case SimMode.TreeGrowth:
                    FruitTreeBehaviorHandler.UpdateTree(ref tree, PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space), Registry);
                    if (tree.IsMature && tree.TimeSinceLastSpawn == 0)
                    {
                        TryGenerateNewTreePosition(tree, newTreePositions);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Registry.Entities[i] = tree;
        }
        
        foreach (var newTreePosition in newTreePositions)
        {
            var physicalTree = new DataTree();
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
    public override void _Process(double delta)
    {
        if (!_running) return;
        
        if (SimulationWorld.PerformanceTest)
        {
            _processStopwatch.Restart();
            _visualUpdateStopwatch.Restart();
        }

        if (VisualTreeRegistry != null)
        {
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
                
                if (physicalTree.FruitGrowthProgress > FruitTreeBehaviorHandler.NodeFruitGrowthDelay && !visualTree.HasFruit)
                {
                    visualTree.GrowFruit(FruitTreeBehaviorHandler.FruitGrowthTime - FruitTreeBehaviorHandler.NodeFruitGrowthDelay);
                }
                visualTree.UpdateTransform(physicalTree);

                VisualTreeRegistry.Entities[i] = visualTree;
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
    #endregion

    #region Performance testing

    // Performance testing
    private Stopwatch _stepStopwatch = new Stopwatch();
    private Stopwatch _processStopwatch = new Stopwatch();
    private Stopwatch _visualUpdateStopwatch = new Stopwatch();
    private double _totalStepTime;
    private double _totalProcessTime;
    private double _totalVisualUpdateTime;
    private int _stepCount;
    private int _processCount;
    
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

    #endregion

    #region Behaviors

    private void TryGenerateNewTreePosition(DataTree parent, List<Vector3> newTrees)
    {
        var angle = SimulationWorld.Rng.RangeFloat(0, Mathf.Tau);
        var distance = SimulationWorld.Rng.RangeFloat(FruitTreeBehaviorHandler.MinTreeSpawnRadius, FruitTreeBehaviorHandler.MaxTreeSpawnRadius);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
        var newPosition = parent.Position + offset;

        if (SimulationWorld.IsWithinWorldBounds(newPosition))
        {
            newTrees.Add(newPosition);
        }
    }

    #endregion

    #region Registry interactions

    private void RegisterTree(DataTree dataTree)
    {
        Registry.RegisterEntity(dataTree);
        VisualTreeRegistry?.RegisterEntity(dataTree);
    }
    public void ClearDeadTrees()
    {
        // TODO: Probably best to make the registries take care of this. They do need to be triggered together, though.
        
        for (var i = Registry.Entities.Count - 1; i >= 0; i--)
        {
            if (!Registry.Entities[i].IsDead) continue;
			     
            Registry.Entities[i].CleanUp();
            Registry.Entities.RemoveAt(i);
        
            if (VisualTreeRegistry != null && VisualTreeRegistry.Entities.Count > 0)
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

    #endregion
    
}
