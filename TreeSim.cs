using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using PrimerTools;
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
    private const float TreeMaturationTime = 1f;
    private const float TreeSpawnInterval = 0.4f;
    private const float MaxTreeSpawnRadius = 5f;
    private const float MinTreeSpawnRadius = 1f;
    private const float TreeCompetitionRadius = 3f;
    private const float MinimumTreeDistance = 0.5f;
    private const float SaplingDeathProbabilityBase = 0.001f;
    private const float SaplingDeathProbabilityPerNeighbor = 0.01f;
    private const float MatureTreeDeathProbabilityBase = 0.0001f;
    private const float MatureTreeDeathProbabilityPerNeighbor = 0.0002f;
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
        var newTreePositions = new List<Vector3>();

        for (var i = 0; i < Registry.PhysicalTrees.Count; i++)
        {
            var tree = Registry.PhysicalTrees[i];
            tree.Age += 1f / PhysicsStepsPerSimSecond;
            
            if (!tree.IsMature)
            { 
                var neighborCount = CountNeighbors(tree);
                var deathProbability = SaplingDeathProbabilityBase + neighborCount * SaplingDeathProbabilityPerNeighbor;
                var dead = false;

                // Check if sapling is too close to a mature tree
                if (IsTooCloseToMatureTree(tree))
                {
                    treesToRemove.Add(i);
                    dead = true;
                }
                else if (_rng.rand.NextDouble() < deathProbability)
                {
                    treesToRemove.Add(i);
                    dead = true;
                }
                
                // Check for maturation
                if (!dead && tree.Age >= TreeMaturationTime)
                {
                    tree.IsMature = true;
                    var transform = Transform3D.Identity.Translated(tree.Position);
                    transform = transform.ScaledLocal(Vector3.One * 1.0f);
                    RenderingServer.InstanceSetTransform(Registry.VisualTrees[i].BodyMesh, transform);
                }
            }
            else
            {
                tree.TimeSinceLastSpawn += 1f / PhysicsStepsPerSimSecond;
                if (tree.TimeSinceLastSpawn >= TreeSpawnInterval)
                {
                    tree.TimeSinceLastSpawn = 0;
                    TryGenerateNewTreePosition(tree, newTreePositions);
                }

                var neighborCount = CountNeighbors(tree);
                var deathProbability = MatureTreeDeathProbabilityBase + neighborCount * MatureTreeDeathProbabilityPerNeighbor;
                if (_rng.rand.NextDouble() < deathProbability)
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
        foreach (var newTreePosition in newTreePositions)
        {
            Registry.CreateTree(newTreePosition, _render);
        }

        return true;
    }

    private void TryGenerateNewTreePosition(TreeSimEntityRegistry.PhysicalTree parent, List<Vector3> newTrees)
    {
        var angle = _rng.RangeFloat(0, Mathf.Tau);
        var distance = _rng.RangeFloat(MinTreeSpawnRadius, MaxTreeSpawnRadius);
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
        return intersections.Count - 1; // Subtract 1 to exclude self
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
            if (intersectedBody != sapling.Body)
            {
                var index = Registry.PhysicalTrees.FindIndex(tree => tree.Body == intersectedBody);
                if (index != -1 && Registry.PhysicalTrees[index].IsMature)
                {
                    return true;
                }
            }
        }
        
        return false;
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

    private void Reset()
    {
        _stepsSoFar = 0;
        Registry.Reset();
    }
}
