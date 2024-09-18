using System.Collections.Generic;
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
    #endregion
    
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

    public const int PhysicsStepsPerSimSecond = 60;

    private static Rng _rng;
    public static Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
    public World3D World3D => GetWorld3D();
    private List<Simulation> _simulations = new();

    public void ResetSimulations()
    {
	    foreach (var simulation in _simulations)
        {
            simulation.Running = false;
		    simulation.Reset();
	    }
    }
    public void Initialize()
    {
        PhysicsServer3D.SetActive(true);
        Engine.PhysicsTicksPerSecond = (int) (_timeScale * 60);

        _simulations.Clear();
        foreach (var child in GetChildren())
        {
            if (child is Simulation simulation)
            {
                _simulations.Add(simulation);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        foreach (var simulation in _simulations)
        {
            if (!simulation.Running) continue;
            simulation.Step();
        }
    }

    public override void _Process(double delta)
    {
        if (!Running) return;
        foreach (var simulation in _simulations)
        {
            if (!simulation.Running) continue;
            simulation.VisualProcess(delta);
        }
    }

    public static bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= _worldDimension.X &&
               position.Z >= 0 && position.Z <= _worldDimension.Y;
    }
}


