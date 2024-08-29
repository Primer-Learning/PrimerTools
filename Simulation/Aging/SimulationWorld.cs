using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using PrimerTools;

namespace PrimerTools.Simulation.Aging;

[Tool]
public partial class SimulationWorld : Node3D
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
			    GD.Print("Sim world is running.");
			    Initialize();
			    GetNode<SimulationGraph>("Sim grapher").Plotting = true;
		    }
    		else if (_running) // This is here so we only do this when stopping a running sim. Not when this gets called on build.
		    {
			    GetNode<SimulationGraph>("Sim grapher").Plotting = false;
			    GD.Print("Sim world is paused");
		    }
		    _running = value;
	    }
    }
    private bool _resetButton = true;
    [Export]
    private bool ResetButton
    {
    	get => _resetButton;
    	set
    	{
    		if (!value && _resetButton && Engine.IsEditorHint())
    		{
    			ResetSimulations();
		    }
    		_resetButton = true;
    	}
    }

    private bool _render = true; 
    [Export]
    private bool Render
    {
        get => _render;
        set
        {
            foreach (var simulation in _simulations)
            {
                simulation.Render = value;
            }

            _render = value;
        }
    }
    [Export] private bool _verbose;
    private Stopwatch _stopwatch;
    #endregion
    
    
    [Export] public Vector2 WorldDimensions = Vector2.One * 50;
    [Export] public int PhysicsStepsPerRealSecond = 60;
    [Export] private int _seed = -1;

    public const int PhysicsStepsPerSimSecond = 60;

    private Rng _rng;
    public Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);

    public World3D World3D => GetWorld3D();

    private List<ISimulation> _simulations = new List<ISimulation>();

    private void Initialize()
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
        if (!_running) return;
        foreach (var simulation in _simulations)
        {
            simulation.Step();
        }
    }

    private void ResetSimulations()
    {
        foreach (var simulation in _simulations)
        {
            simulation.Reset();
        }
    }

    public bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= WorldDimensions.X &&
               position.Z >= 0 && position.Z <= WorldDimensions.Y;
    }
}

