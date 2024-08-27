using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using PrimerTools.Simulation.Tree;

[Tool]
public partial class TreeSim : Node3D
{
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
                _stepsSoFar = 0;
                if (_stopwatch != null)
                {
                    _stopwatch.Stop();
                    GD.Print($"Elapsed time: {_stopwatch.Elapsed}");
                }
                Reset();
            }
            
            _running = value;
        }
    }
    private bool _resetUpButton = true;
    [Export]
    private bool ResetButton
    {
        get => _resetUpButton;
        set
        {
            if (!value && _resetUpButton && Engine.IsEditorHint())
            {
                Reset();
            }
            _resetUpButton = true;
        }
    }

    [Export] private bool _render = true;
    [Export] private bool _verbose;
    private Stopwatch _stopwatch;
    #endregion
    
    #region Sim parameters
    private Rng _rng;
    [Export] private int _seed = -1;
    [Export] private int _initialTreeCount = 5;
    [Export] private Vector2 _worldDimensions = Vector2.One * 50;
    [Export] private int _maxNumSteps = 1000;
    [Export] private int _physicsStepsPerRealSecond = 60;
    private const int PhysicsStepsPerSimSecond = 60;
    private const float TreeMaturationTime = 5f;
    private const float TreeSpawnInterval = 2f;
    private const float TreeSpawnRadius = 5f;
    private const float TreeCompetitionRadius = 3f;
    private const float TreeDeathProbabilityBase = 0.001f;
    private const float TreeDeathProbabilityPerNeighbor = 0.0005f;
    private int _stepsSoFar = 0;
    #endregion
    
    public TreeSimEntityRegistry Registry = new();

    private void Initialize()
    {
        Registry.World3D = GetWorld3D();
        _stopwatch = Stopwatch.StartNew();
        
        _rng = new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
        PhysicsServer3D.SetActive(true);
        Engine.PhysicsTicksPerSecond = _physicsStepsPerRealSecond;

        for (var i = 0; i < _initialTreeCount; i++)
        {
            Registry.CreateTree(
                new Vector3(
                    _rng.RangeFloat(_worldDimensions.X),
                    0,
                    _rng.RangeFloat(_worldDimensions.Y)
                ),
                _render
            );
        }
    }

    private bool Step()
    {
        if (Registry.PhysicalTrees.Count == 0)
        {
            GD.Print("No Trees found. Stopping.");
            Running = false;
            return false;
        }

        var treesToRemove = new List<int>();
        var newTrees = new List<TreeSimEntityRegistry.PhysicalTree>();

        for (var i = 0; i < Registry.PhysicalTrees.Count; i++)
        {
            var tree = Registry.PhysicalTrees[i];
            tree.Age += 1f / PhysicsStepsPerSimSecond;

            if (!tree.IsMature && tree.Age >= TreeMaturationTime)
            {
                tree.IsMature = true;
            }

            if (tree.IsMature)
            {
                tree.TimeSinceLastSpawn += 1f / PhysicsStepsPerSimSecond;
                if (tree.TimeSinceLastSpawn >= TreeSpawnInterval)
                {
                    tree.TimeSinceLastSpawn = 0;
                    TrySpawnNewTree(tree, newTrees);
                }

                var neighborCount = CountNeighbors(tree);
                var deathProbability = TreeDeathProbabilityBase + neighborCount * TreeDeathProbabilityPerNeighbor;
                if (_rng.RangeFloat(0, 1) < deathProbability)
                {
                    treesToRemove.Add(i);
                }
            }

            Registry.PhysicalTrees[i] = tree;
        }

        // Remove dead trees
        for (int i = treesToRemove.Count - 1; i >= 0; i--)
        {
            int index = treesToRemove[i];
            Registry.PhysicalTrees[index].FreeRids();
            if (_render)
            {
                Registry.VisualTrees[index].FreeRids();
                Registry.VisualTrees.RemoveAt(index);
            }
            Registry.PhysicalTrees.RemoveAt(index);
        }

        // Add new trees
        foreach (var newTree in newTrees)
        {
            Registry.CreateTree(newTree.Position, _render);
        }

        return true;
    }

    private void TrySpawnNewTree(TreeSimEntityRegistry.PhysicalTree parent, List<TreeSimEntityRegistry.PhysicalTree> newTrees)
    {
        var angle = _rng.RangeFloat(0, Mathf.Tau);
        var distance = _rng.RangeFloat(0, TreeSpawnRadius);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
        var newPosition = parent.Position + offset;

        if (IsWithinWorldBounds(newPosition))
        {
            newTrees.Add(new TreeSimEntityRegistry.PhysicalTree
            {
                Position = newPosition,
                Age = 0,
                IsMature = false,
                TimeSinceLastSpawn = 0
            });
        }
    }

    private int CountNeighbors(TreeSimEntityRegistry.PhysicalTree tree)
    {
        var queryParams = new PhysicsShapeQueryParameters3D();
        queryParams.CollideWithAreas = true;
        queryParams.ShapeRid = PhysicsServer3D.AreaGetShape(tree.Body, 0);
        var transform = Transform3D.Identity.Translated(tree.Position);
        transform = transform.Scaled(Vector3.One * TreeCompetitionRadius);
        queryParams.Transform = transform;

        var intersections = PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space).IntersectShape(queryParams);
        return intersections.Count - 1; // Subtract 1 to exclude self
    }

    private bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= _worldDimensions.X &&
               position.Z >= 0 && position.Z <= _worldDimensions.Y;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_running) return;
        if (_stepsSoFar >= _maxNumSteps)
        {
            GD.Print("Done");
            Running = false;
            return;
        }
        
        if (Step()) _stepsSoFar++;
        if (_verbose && _stepsSoFar % 100 == 0) GD.Print($"Finished step {_stepsSoFar}");
    }

    public override void _Process(double delta)
    {
        if (!_running || !_render) return;

        for (var i = 0; i < Registry.PhysicalTrees.Count; i++)
        {
            var physicalTree = Registry.PhysicalTrees[i];
            var visualTree = Registry.VisualTrees[i];

            var transform = Transform3D.Identity.Translated(physicalTree.Position);
            transform = transform.Scaled(Vector3.One * (physicalTree.IsMature ? 1.0f : 0.5f));
            RenderingServer.InstanceSetTransform(visualTree.BodyMesh, transform);
        }
    }

    private void Reset()
    {
        _stepsSoFar = 0;
        Registry.Reset();
    }
}
