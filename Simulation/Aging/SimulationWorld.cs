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
		    }
    		else if (_running) // This is here so we only do this when stopping a running sim. Not when this gets called on build.
		    {
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
    			_creatureSim.Reset();
			    _treeSim.Reset();
		    }
    		_resetButton = true;
    	}
    }

    [Export] private bool _render;
    [Export] private bool _verbose;
    private Stopwatch _stopwatch;
    #endregion
    
    
    [Export] public Vector2 WorldDimensions = Vector2.One * 50;
    [Export] public int PhysicsStepsPerRealSecond = 60;
    [Export] private int _seed = -1;

    public const int PhysicsStepsPerSimSecond = 60;

    private Rng _rng;
    public Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);

    public World3D World3D { get; private set; }

    private CreatureSim _creatureSim;
    private TreeSim _treeSim;
    private void Initialize()
    {
        World3D = GetWorld3D();
        PhysicsServer3D.SetActive(true);
        Engine.PhysicsTicksPerSecond = PhysicsStepsPerRealSecond;

        _creatureSim = GetNode<CreatureSim>("Creature Sim");
        _treeSim = GetNode<TreeSim>("Forest Sim");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_running) return;
        _treeSim.Step();
        _creatureSim.Step();
    }

    public bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= WorldDimensions.X &&
               position.Z >= 0 && position.Z <= WorldDimensions.Y;
    }
}
