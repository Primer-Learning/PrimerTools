using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using PrimerTools;

namespace PrimerTools.Simulation.Aging;

[Tool]
public partial class SimulationWorld : Node3D
{
    #region Editor controls
    // private bool _running;
    // [Export]
    public bool Running;

    [Export] public bool Render;
    #endregion
    
    [Export] public Vector2 WorldDimensions = Vector2.One * 50;
    [Export] public int PhysicsStepsPerRealSecond = 60;
    [Export] private int _seed = -1;

    public const int PhysicsStepsPerSimSecond = 60;

    private Rng _rng;
    public Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);

    public World3D World3D => GetWorld3D();

    private List<ISimulation> _simulations = new List<ISimulation>();

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
        Engine.PhysicsTicksPerSecond = PhysicsStepsPerRealSecond;

        _simulations.Clear();
        foreach (var child in GetChildren())
        {
            if (child is ISimulation simulation)
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
            simulation.Step();
        }
    }

    public bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= WorldDimensions.X &&
               position.Z >= 0 && position.Z <= WorldDimensions.Y;
    }
}

