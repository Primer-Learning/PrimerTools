using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public enum VisualizationMode
{
    None,
    NodeCreatures
}

[Tool]
public partial class SimulationWorld : Node3D
{
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

    private float _timeScale = 1;
    [Export]
    public float TimeScale
    {
        get => _timeScale;
        set
        {
            _timeScale = value;
            Engine.PhysicsTicksPerSecond = (int)(value * 60);
        }
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

    private static Rng _rng;
    public static Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
    public World3D World3D => GetWorld3D();
    public readonly List<ISimulation> Simulations = new();
    private NodeCreatureManager _creatureNodeManager;
    private NodeTreeManager _treeNodeManager;

    public void ResetSimulations()
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
        
        Simulations.Clear();
        var creatureSim = new CreatureSim(this);
        Simulations.Add(creatureSim);
        var treeSim = new FruitTreeSim(this);
        Simulations.Add(treeSim);
        
        if (VisualizationMode == VisualizationMode.NodeCreatures)
        {
            _treeNodeManager = new NodeTreeManager(treeSim.Registry);
            _treeNodeManager.Name = "NodeTreeAnimationManager";
            AddChild(_treeNodeManager);
            
            _creatureNodeManager = new NodeCreatureManager(creatureSim.Registry, _treeNodeManager);
            _creatureNodeManager.Name = "NodeCreatureAnimationManager";
            AddChild(_creatureNodeManager);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        foreach (var simulation in Simulations)
        {
            simulation.Step();
        }
    }

    private double _statusPrintInterval = 1;
    private double _timeSinceLastStatusPrint;
    public override void _Process(double delta)
    {
        if (!Running) return;
        
        _creatureNodeManager?.VisualProcess(delta);
        _treeNodeManager?.VisualProcess(delta);
        
        foreach (var simulation in Simulations)
        {
            simulation.ClearDeadEntities();
        }

        if (VisualizationMode != VisualizationMode.None) return;
        _timeSinceLastStatusPrint += delta;
        if (!(_timeSinceLastStatusPrint > _statusPrintInterval)) return;
        GD.Print($"Trees: {Simulations.OfType<FruitTreeSim>().FirstOrDefault().Registry.Entities.Count(x => x.Alive)}");
        GD.Print($"Creatures: {Simulations.OfType<CreatureSim>().FirstOrDefault().Registry.Entities.Count(x => x.Alive)}");
    }

    public static bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= _worldDimension.X &&
               position.Z >= 0 && position.Z <= _worldDimension.Y;
    }
}


