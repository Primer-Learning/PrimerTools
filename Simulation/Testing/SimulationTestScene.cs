using System.Collections.Generic;
using Godot;
using System.Linq;
using System.Threading.Tasks;
using PrimerTools.Graph;
using PrimerTools.Simulation;

[Tool]
public partial class SimulationTestScene : Node3D
{
	private SimulationWorld SimulationWorld => GetNode<SimulationWorld>("SimulationWorld");
	private CreatureSim CreatureSim => SimulationWorld.Simulations.OfType<CreatureSim>().FirstOrDefault();
	private FruitTreeSim FruitTreeSim => SimulationWorld.Simulations.OfType<FruitTreeSim>().FirstOrDefault();
	
	private bool _newSim = true;
	private bool _running;
	[Export] private bool Run {
		get => _running;
		set {
			if (value)
			{
				if (_newSim)
				{
					GD.Print("Running new sim");
					if (_plotting) CreatePlot();
					RunSimSequence();
					_newSim = false;
				}
				else
				{
					GD.Print("Continuing sim");
					SimulationWorld.Running = true;
					if (PeriodicPlotter != null)
					{
						PeriodicPlotter.Plotting = true;
					}
					if (PeriodicPlotter2 != null)
					{
						PeriodicPlotter2.Plotting = true;
					}
					if (PeriodicPlotter3 != null)
					{
						PeriodicPlotter3.Plotting = true;
					}
				}
			}
			else if (_running)
			{
				GD.Print("Pausing");
				SimulationWorld.Running = false;
				if (PeriodicPlotter != null) PeriodicPlotter.Plotting = false;
				if (PeriodicPlotter2 != null) PeriodicPlotter2.Plotting = false;
				if (PeriodicPlotter3 != null) PeriodicPlotter3.Plotting = false;
			}
			_running = value;
		}
	}
	
	private bool _reset;
	[Export] private bool Reset {
		get => _reset;
		set {
			if (value)
			{
				GD.Print("Resetting");
				SimulationWorld.ResetSimulations();
				SimulationWorld.Running = false;
				
				foreach (var child in GraphParent.GetChildren())
				{
					if (IsInstanceValid(child)) child.Free();
				}

				_newSim = true;
				// SimulationWorld.TimeScale = 1;
			}
			_reset = false;
		}
	}

	[Export] private bool _plotting;
	[Export] private int _initialCreatureCount = 2;
	[Export] private int _initialTreeCount = 10;
	
	private Node3D GraphParent => GetNode<Node3D>("GraphParent");
	private PeriodicPlotter PeriodicPlotter => GraphParent.GetNodeOrNull<PeriodicPlotter>("Periodic plotter");
	private PeriodicPlotter PeriodicPlotter2 => GraphParent.GetNodeOrNull<PeriodicPlotter>("Periodic plotter2");
	private PeriodicPlotter PeriodicPlotter3 => GraphParent.GetNodeOrNull<PeriodicPlotter>("Periodic plotter3");
	
	private void CreatePlot()
	{
		foreach (var child in GraphParent.GetChildren())
		{
			if (IsInstanceValid(child)) child.Free();
		}
		
		var thisGraph = Graph.CreateInstance();
		thisGraph.Name = "Graph1";
		GraphParent.AddChild(thisGraph);
		// thisGraph.Owner = GetTree().EditedSceneRoot;
		thisGraph.XAxis.length = 60;
		thisGraph.XAxis.Max = 40;
		thisGraph.XAxis.TicStep = 20;
		
		thisGraph.YAxis.length = 50;
		
		// TODO: Give periodic plotter the option of automatically adjusting ranges 
		thisGraph.YAxis.Max = 1f;
		thisGraph.YAxis.TicStep = 0.2f;
		
		// thisGraph.YAxis.Max = 40f;
		// thisGraph.YAxis.TicStep = 10f;
		
		thisGraph.ZAxis.length = 0;

		const int thisChonk = 25;
		thisGraph.XAxis.Chonk = thisChonk;
		thisGraph.YAxis.Chonk = thisChonk;
		thisGraph.ZAxis.Chonk = thisChonk;
		thisGraph.Transition();

		var periodicPlotter = new PeriodicPlotter();
		GraphParent.AddChild(periodicPlotter);
		periodicPlotter.Name = "Periodic plotter";
		
		// Curve
		// var curve = thisGraph.AddCurvePlot2D();
		// curve.Width = 200;
		// curve.DataFetchMethod = CurvePlot2DUtilities.AppendAverageProperty<DataCreature>(
		// 	() => CreatureSim.Registry.Entities,
		// 	curve,
		// 	x => x.MaxAge
		// );
		// curve.DataFetchMethod = CurvePlot2DUtilities.AppendCount(() => CreatureSim.Registry.Entities, curve);
		// periodicPlotter.Curve = curve;
		
		// Bar plot
		var barPlot = thisGraph.AddBarPlot();
		
		// Cumulative death/survival plotting
		// var creatureDeathAges = new List<float>();
		// CreatureSim.CreatureDeathEvent += (int index) =>
		// {
		// 	creatureDeathAges.Add(CreatureSim.Registry.Entities[index].Age);
		// };
		//
		// barPlot.DataFetchMethod = () =>
		// {
		// 	// Old max age plotting
		// 	// var values = CreatureSim.Registry.Entities.Select(x => x.MaxAge).ToList();
		// 	
		// 	var histogram = BarDataUtilities.MakeHistogram(creatureDeathAges, 1);
		// 	
		// 	// Normalize
		// 	var floatHistogram = histogram.Select(x => x / creatureDeathAges.Count).ToArray();
		//
		// 	var transformedHistogram = new List<float>();
		// 	var survival = 1f;
		// 	foreach (var val in floatHistogram)
		// 	{
		// 		survival -= val;
		// 		transformedHistogram.Add(survival);
		// 	}
		// 	
		// 	return transformedHistogram.ToArray();
		// };

		// Max age histogram
		barPlot.DataFetchMethod = BarDataUtilities.NormalizedPropertyHistogram(() => CreatureSim.Registry.Entities, x => x.MaxAge);
		// TODO: Get this to actually count alleles. Could allow it to take delegate returning an array of floats instead of just a single float
		// barPlot.DataFetchMethod = BarDataUtilities.NormalizedPropertyHistogram(() => CreatureSim.Registry.Entities, x => x.Genome.GetTrait<float>("MaxAge").Alleles /*...*/);
		
		periodicPlotter.BarPlot = barPlot;
		
		
		//
		// Second graph
		//
		
		var thisGraph2 = Graph.CreateInstance();
		thisGraph2.Name = "Graph2";
		GraphParent.AddChild(thisGraph2);
		// thisGraph2.Owner = GetTree().EditedSceneRoot;
		thisGraph2.XAxis.length = 60;
		thisGraph2.XAxis.Max = 40;
		thisGraph2.XAxis.TicStep = 20;
		thisGraph2.Position = Vector3.Right * 90;
		
		thisGraph2.YAxis.length = 50;
		
		// TODO: Give periodic plotter the option of automatically adjusting ranges 
		thisGraph2.YAxis.Max = 1f;
		thisGraph2.YAxis.TicStep = 0.2f;
		
		// thisGraph.YAxis.Max = 40f;
		// thisGraph.YAxis.TicStep = 10f;
		
		thisGraph2.ZAxis.length = 0;

		thisGraph2.XAxis.Chonk = thisChonk;
		thisGraph2.YAxis.Chonk = thisChonk;
		thisGraph2.ZAxis.Chonk = thisChonk;
		thisGraph2.Transition();

		var periodicPlotter2 = new PeriodicPlotter();
		GraphParent.AddChild(periodicPlotter2);
		periodicPlotter2.Name = "Periodic plotter2";
		
		// Bar plot
		var barPlot2 = thisGraph2.AddBarPlot();
		
		// Cumulative death/survival plotting
		var creatureDeathAges = new List<float>();
		CreatureSim.CreatureDeathEvent += index =>
		{
			var age = CreatureSim.Registry.Entities[index].Age;
			creatureDeathAges.Add(age);
			// if (creatureDeathAges.Count > 1000) creatureDeathAges.RemoveAt(0);
		};
		
		barPlot2.DataFetchMethod = () =>
		{
			var histogram = BarDataUtilities.MakeHistogram(creatureDeathAges, 1);
			
			// Normalize
			histogram = histogram.Select(x => x / creatureDeathAges.Count).ToArray();
		
			// Transform to cumulative survival
			var transformedHistogram = new List<float>();
			var survival = 1f;
			foreach (var val in histogram)
			{
				survival -= val;
				transformedHistogram.Add(survival);
			}
			
			return transformedHistogram.ToArray();
		};
		
		periodicPlotter2.BarPlot = barPlot2;
		
		
		
		
		//
		// Third graph
		//
		
		var thisGraph3 = Graph.CreateInstance();
		thisGraph3.Name = "Graph3";
		GraphParent.AddChild(thisGraph3);
		// thisGraph3.Owner = GetTree().EditedSceneRoot;
		thisGraph3.XAxis.length = 60;
		thisGraph3.XAxis.Max = 40;
		thisGraph3.XAxis.TicStep = 20;
		thisGraph3.Position = Vector3.Right * 180;
		
		thisGraph3.YAxis.length = 50;
		
		thisGraph3.YAxis.Max = 1f;
		thisGraph3.YAxis.TicStep = 0.2f;
		
		// thisGraph.YAxis.Max = 40f;
		// thisGraph.YAxis.TicStep = 10f;
		
		thisGraph3.ZAxis.length = 0;

		thisGraph3.XAxis.Chonk = thisChonk;
		thisGraph3.YAxis.Chonk = thisChonk;
		thisGraph3.ZAxis.Chonk = thisChonk;
		thisGraph3.Transition();

		var periodicPlotter3 = new PeriodicPlotter();
		GraphParent.AddChild(periodicPlotter3);
		periodicPlotter3.Name = "Periodic plotter2";
		
		// Bar plot
		var barPlot3 = thisGraph3.AddBarPlot();
		
		barPlot3.DataFetchMethod = () =>
		{
			var histogram = BarDataUtilities.MakeHistogram(creatureDeathAges, 1);
			
			// Normalize
			histogram = histogram.Select(x => x / creatureDeathAges.Count).ToArray();
		
			// Transform to cumulative survival
			var transformedHistogram = new List<float>();
			var survival = 1f;
			foreach (var val in histogram)
			{
				survival -= val;
				transformedHistogram.Add(survival);
			}
			
			// Transform to death probability at age, given survival to that age
			var evenMoreTransformedHistogram = new List<float>();
			for (var i = 0; i < transformedHistogram.Count; i++)
			{
				if (i == 0) evenMoreTransformedHistogram.Add(0);
				else
				{
					var value = transformedHistogram[i];
					var prevValue = transformedHistogram[i - 1];
					evenMoreTransformedHistogram.Add(1 - value / prevValue);
				}
			}
			
			return evenMoreTransformedHistogram.ToArray();
		};
		
		periodicPlotter3.BarPlot = barPlot3;
	}

	private async Task RunSimSequence()
	{
		// TODO: Figure out an actual good approach to time and step counts
		// Step counts are important for consistent results. But timing is important for perception.
		// Probably always set timings in terms of total steps.
		// Waiting could happen a number of ways. The current way waits 100 ms between checks, which will be imprecise.
		// Could await every physics frame? But that seems silly. Maybe it's not silly of GetPhysicsFrames is cheap.
		// We do unfortunately have to use an async method because the physics update needs to run between sim steps.
		
		var startPhysicsFrame = Engine.GetPhysicsFrames();
		
		var originalTimeScale = SimulationWorld.TimeScale;
		SimulationWorld.TimeScale = 2;
		SimulationWorld.Initialize();
		SimulationWorld.Running = true;
		
		FruitTreeSim.Mode = FruitTreeSim.SimMode.TreeGrowth;
		FruitTreeSim.InitialEntityCount = _initialTreeCount;
		FruitTreeSim.Initialize();

		const ulong treeGrowthStepGoal = 300;
		
		while (Engine.GetPhysicsFrames() < startPhysicsFrame + treeGrowthStepGoal) await Task.Delay(100);
		while (!_running) await Task.Delay(100);
		
		SimulationWorld.TimeScale = originalTimeScale;

		FruitTreeSim.Mode = FruitTreeSim.SimMode.FruitGrowth;

		await Task.Delay(3000);

		CreatureSim.InitialEntityCount = _initialCreatureCount;
		CreatureSim.Initialize();
		if (PeriodicPlotter != null) PeriodicPlotter.Plotting = true;
		if (PeriodicPlotter2 != null) PeriodicPlotter2.Plotting = true;
		if (PeriodicPlotter3 != null) PeriodicPlotter3.Plotting = true;
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint()) return;
		Run = true;
		Engine.MaxFps = 0;
	}
}
