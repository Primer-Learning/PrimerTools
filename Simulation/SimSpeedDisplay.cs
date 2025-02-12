using Godot;
using PrimerTools;
using PrimerTools.LaTeX;
using PrimerTools.Simulation;

public partial class SimSpeedDisplay : Node3D
{
	// TODO: Decide whether to keep this. It's kind of nice, but it would need to go to zero when the sim world is paused.
	// For now, I commented out the parts that make it follow the sim world, and instead made the update method public.
	// 
	
	// private SimulationWorld _simulationWorld;
	public LatexNode _latexNode;
	public float MultiplierForLying = 1;
	private int _initialValue;

	public SimSpeedDisplay(int initialValue)
	{
		// _simulationWorld = simulationWorld; 
		_initialValue = initialValue;
	}

	public override void _Ready()
	{
		base._Ready();
		// SimulationWorld.TimeScaleChanged += UpdateDisplay;

		_latexNode = new LatexNode();
		_latexNode.numberPrefix = "\\times";
		_latexNode.NumericalExpression = _initialValue;
		_latexNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Left;
		AddChild(_latexNode);
		Name = "Sim speed display";
		// UpdateDisplay(_simulationWorld.TimeScaleControl);
	}
	
	public void UpdateDisplay(float speed, double duration = AnimationUtilities.DefaultDuration)
	{ 
		GD.Print("Updating speed display");
		if (!IsInstanceValid(this))
		{
			GD.PushWarning("Invalid SimSpeedDisplay");
			return;
		}
		var tween = CreateTween();
		tween.TweenProperty(
			_latexNode,
			"NumericalExpression",
			speed * MultiplierForLying,
			duration
		);
	}

	public void SetToDefaultPosition()
	{
		Position = new Vector3(-19.6f, -10.3f, -43.4f);
	}
}
