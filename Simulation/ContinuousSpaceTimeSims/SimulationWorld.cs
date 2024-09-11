using System.Collections.Generic;
using System.Diagnostics;
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
    
    [Export] public Vector2 WorldDimensions = Vector2.One * 50;

    private float _timeScale = 1;
    // [Export]
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

    [Export] // Would make this static, but you can't export a static property. And I want to control this from the editor.
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

    public bool Testing;
    public bool PerformanceTest;
    private int _stepsSoFar = 0;
    private Stopwatch _stopwatch;
    private int _maxSteps = 3000;

    private static Rng _rng;
    public static Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
    public World3D World3D => GetWorld3D();
    private List<ISimulation> _simulations = new();

    public void ResetSimulations()
    {
	    foreach (var simulation in _simulations)
	    {
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
            if (child is ISimulation simulation)
            {
                _simulations.Add(simulation);
            }
        }

        _stepsSoFar = 0;
        _stopwatch = Stopwatch.StartNew();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        foreach (var simulation in _simulations)
        {
            simulation.Step();
        }
        _stepsSoFar++;
        
        // For speed tests
        if (!Testing || _stepsSoFar < _maxSteps) return;
        Running = false;
        GD.Print($"Elapsed time = {_stopwatch.ElapsedMilliseconds} milliseconds");
    }

    public bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= WorldDimensions.X &&
               position.Z >= 0 && position.Z <= WorldDimensions.Y;
    }
}


