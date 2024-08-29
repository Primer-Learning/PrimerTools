using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aging.addons.PrimerTools.Simulation.Aging;
using PrimerTools;
using PrimerTools.Graph;

[Tool]
public partial class SimulationTestGraph : Node3D
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
	
	private CreatureSimEntityRegistry Registry => GetParent<Node3D>().GetNode<CreatureSim>("Creature Sim").Registry;
	private CurvePlot2D _curve;
	private Graph SimGraph => GetNode<Graph>("Graph");
	private PeriodicPlotter PeriodicPlotter => GetNode<PeriodicPlotter>("Periodic plotter");
	
	private void CreatePlot()
	{
		SimGraph?.Free();
		PeriodicPlotter?.Free();
		
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

		_curve.DataFetchMethod = () =>
		{
			var dataList = _curve.GetData().ToList();
			dataList.Add( new Vector3(dataList.Count, Registry.PhysicalCreatures.Count, 0) );
			return dataList;
		};

		var periodicPlotter = new PeriodicPlotter();
		AddChild(periodicPlotter);
		periodicPlotter.Owner = GetTree().EditedSceneRoot;
		periodicPlotter.Name = "Periodic plotter";
		periodicPlotter.Curve = _curve;
	}
}
