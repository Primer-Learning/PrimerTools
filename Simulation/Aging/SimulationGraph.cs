using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aging.addons.PrimerTools.Simulation.Aging;
using PrimerTools;
using PrimerTools.Graph;

[Tool]
public partial class SimulationGraph : Node3D
{
	
	private bool _run;
	[Export] private bool RunButton {
		get => _run;
		set {
			if (!value && _run && Engine.IsEditorHint())
			{
				CreatePlot();
			}
			_run = true;
		}
	}

	private Graph SimGraph => GetNode<Graph>("Graph");
	private CurvePlot2D _curve;
	private List<Vector3> _creatureCounts = new();
	private void CreatePlot()
	{
		SimGraph?.Free();
		var thisGraph = Graph.CreateInstance();
		AddChild(thisGraph);
		thisGraph.Owner = GetTree().EditedSceneRoot;
		thisGraph.XAxis.length = 60;
		thisGraph.XAxis.Max = 40;
		thisGraph.XAxis.TicStep = 20;
		
		thisGraph.YAxis.length = 30;
		thisGraph.YAxis.Max = 100;
		thisGraph.YAxis.TicStep = 20;
		
		thisGraph.ZAxis.length = 0;

		var thisChonk = 25;
		thisGraph.XAxis.Chonk = thisChonk;
		thisGraph.YAxis.Chonk = thisChonk;
		thisGraph.ZAxis.Chonk = thisChonk;
		thisGraph.Transition();

		_curve = thisGraph.AddCurvePlot2D();
		_curve.Width = 200;
		
		
	}

	private CreatureSimEntityRegistry Registry => GetParent<Node3D>().GetNode<CreatureSim>("Creature Sim").Registry;
	private void PlotData()
	{
		if (Registry.PhysicalCreatures.Count == 0) return;
		
		_creatureCounts.Add( new Vector3(_creatureCounts.Count, Registry.PhysicalCreatures.Count, 0) );
		GD.Print(_creatureCounts.Last());
		_curve.SetData( _creatureCounts.ToArray() );

		
		
		if (_curve.pointsOfStages.Count == 0)
			_curve.pointsOfStages.Add(new[] { _curve.TransformPointFromDataSpaceToPositionSpace(_curve.GetData()[0]) });
		_curve.pointsOfStages.Add(_curve.GetData().Select(x => _curve.TransformPointFromDataSpaceToPositionSpace(x)).ToArray());
		
		var tween = CreateTween();
		tween.TweenProperty(
			_curve,
			"RenderExtent",
			_curve.RenderExtent + 1,
			1
		);
	}

	public bool Plotting;
	private double _timeSinceLastPlot;
	public override void _Process(double delta)
	{
		if (!Plotting) return;
		_timeSinceLastPlot += delta;
		if (_timeSinceLastPlot < 1) return;
		PlotData();
		_timeSinceLastPlot = 0;
	}
}
