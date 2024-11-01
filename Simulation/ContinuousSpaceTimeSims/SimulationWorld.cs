using System.Collections.Generic;
using Godot;
using PrimerTools.Simulation.New;

namespace PrimerTools.Simulation;

public enum VisualizationMode
{
    None,
    NodeCreatures
}

[Tool]
public partial class SimulationWorld : Node3D
{
    PackedScene _groundScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerAssets/Organized/Ground/round_ground.tscn");
    
    #region Editor controls
    public bool Running;
    [Export] public VisualizationMode VisualizationMode = VisualizationMode.NodeCreatures;
    
    private static Vector2 _worldDimension = Vector2.One * 50;
    [Export]
    public Vector2 WorldDimensions
    {
        get => _worldDimension;
        set => _worldDimension = value;
    }

    public Node3D Ground; 

    private static float _timeScale = 1;
    public static float TimeScale
    {
        get => _timeScale;
        set
        {
            _timeScale = value;
            Engine.PhysicsTicksPerSecond = (int)(value * 60);
        }
    }

    [Export]
    public float TimeScaleControl
    {
        get => TimeScale;
        set => TimeScale = value;
    }
    
    private static int _seed = -1;
    [Export]
    public int Seed
    {
        get => _seed;
        set
        {
            _seed = value;
            _rng = null; // So it will be reconstructed with the new seed next time it's read
        }
    }
    #endregion

    public const int PhysicsStepsPerSimSecond = 60;
    public const float TimeStep = 1f / PhysicsStepsPerSimSecond;

    private Rng _rng;
    public Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
    public World3D World3D => GetWorld3D();
    public readonly List<ISimulation> Simulations = new();
    private NodeCreatureManager _creatureNodeManager;
    private NodeTreeManager _treeNodeManager;

    public void Reset()
    {
        Running = false;
	    foreach (var simulation in Simulations)
        {
		    simulation.Reset();
	    }
        _creatureNodeManager?.QueueFree();
        _creatureNodeManager = null;
        _treeNodeManager?.QueueFree();
        _treeNodeManager = null;
    }
    public void Initialize()
    {
        PhysicsServer3D.SetActive(true);
        Engine.PhysicsTicksPerSecond = (int) (_timeScale * 60);

        if (Ground != null && IsInstanceValid(Ground)) Ground.Free();
        Ground = _groundScene.Instantiate<Node3D>();
        AddChild(Ground);
        Ground.Name = "Ground";
        Ground.Scale = new Vector3(WorldDimensions.Y, (WorldDimensions.X + WorldDimensions.Y) / 2, WorldDimensions.X);
        
        Simulations.Clear();
        
        // TODO: Put this in SimulationTestScene, since it's really a setting for SimulationWorld.

        CreatureSimSettings.Instance.FindMate = MateSelectionStrategies.FindFirstAvailableMate;
        CreatureSimSettings.Instance.Reproduce = ReproductionStrategies.SexualReproduce;
        CreatureSimSettings.Instance.InitializePopulation =
            InitialPopulationGeneration.WorkingInitialPopulationThatChangesALot;
        var creatureSim = new CreatureSim(this);
        Simulations.Add(creatureSim);

        var fruitTreeSimSettings = new FruitTreeSimSettings();
        var treeSim = new FruitTreeSim(this, fruitTreeSimSettings);
        Simulations.Add(treeSim);
        
        if (VisualizationMode == VisualizationMode.NodeCreatures)
        {
            _treeNodeManager = new NodeTreeManager(treeSim.Registry);
            _treeNodeManager.Name = "NodeTreeManager";
            AddChild(_treeNodeManager);
            
            _creatureNodeManager = new NodeCreatureManager(creatureSim.Registry, _treeNodeManager);
            _creatureNodeManager.Name = "NodeCreatureManager";
            AddChild(_creatureNodeManager);
            // _creatureNodeManager.MakeSelfAndChildrenLocal();
        }

        _realTimeOfLastStatusPrint = System.Environment.TickCount;
    }

    private int _realTimeOfLastStatusPrint;
    private int _stepsSinceLastStatusPrint;
    [Export] private float _statusPrintSimTimeInterval;
    private int _totalPhysicsSteps;
    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        foreach (var simulation in Simulations)
        {
            simulation.Step();
        }

        if (_statusPrintSimTimeInterval == 0) return;
        _totalPhysicsSteps++;
        _stepsSinceLastStatusPrint++;

        if ((float)_stepsSinceLastStatusPrint / PhysicsStepsPerSimSecond >= _statusPrintSimTimeInterval)
        {
            var currentTime = System.Environment.TickCount;
            var differenceInSeconds = (currentTime - _realTimeOfLastStatusPrint) / 1000f; 
            GD.Print($"{_statusPrintSimTimeInterval} sim seconds in {differenceInSeconds} real seconds");
            _stepsSinceLastStatusPrint = 0;
            _realTimeOfLastStatusPrint = currentTime;
        }
    }
    
    public override void _Process(double delta)
    {
        if (!Running) return;
        
        _creatureNodeManager?.VisualProcess(delta);
        _treeNodeManager?.VisualProcess(delta);
        
        foreach (var simulation in Simulations)
        {
            simulation.ClearDeadEntities();
        }
    }

    public static bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= _worldDimension.X &&
               position.Z >= 0 && position.Z <= _worldDimension.Y;
    }
    public Vector3 GetRandomDestination(Vector3 position, float maxDistance, float minDistance = 0)
    {
        Vector3 newDestination;
        var attempts = 0;
        const int maxAttempts = 100;
        
        if (minDistance > maxDistance) PrimerGD.PushWarningWithStackTrace("minDistance is greater than maxDistance, ya dummy" );

        do
        {
            var angle = _rng.RangeFloat(1) * 2 * Mathf.Pi;
            var displacement = _rng.RangeFloat(minDistance, maxDistance) * new Vector3(
                Mathf.Sin(angle),
                0,
                Mathf.Cos(angle)
            );
            newDestination = position + displacement;
            attempts++;

            if (attempts >= maxAttempts)
            {
                GD.PrintErr($"Failed to find a valid destination after {maxAttempts} attempts. Using current position.");
                newDestination = position;
                break;
            }
        } while (!IsWithinWorldBounds(newDestination));

        return newDestination;
    }
}


