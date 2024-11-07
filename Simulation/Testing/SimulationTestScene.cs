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
				SimulationWorld.Reset();
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
	private PeriodicPlotter PeriodicPlotter => GraphParent.GetNodeOrNull<PeriodicPlotter>("Graph1/Periodic plotter");
	private PeriodicPlotter PeriodicPlotter2 => GraphParent.GetNodeOrNull<PeriodicPlotter>("Graph2/Periodic plotter2");
	private PeriodicPlotter PeriodicPlotter3 => GraphParent.GetNodeOrNull<PeriodicPlotter>("Graph3/Periodic plotter3");
	
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
		thisGraph.XAxis.Max = 100;
		thisGraph.XAxis.TicStep = 20;
		
		thisGraph.YAxis.length = 50;
		
		// TODO: Give periodic plotter the option of automatically adjusting ranges 
		thisGraph.YAxis.Max = 1f;
		thisGraph.YAxis.TicStep = 0.2f;
		
		// thisGraph.YAxis.Max = 40f;
		// thisGraph.YAxis.TicStep = 10f;
		
		thisGraph.ZAxis.length = 60;
		thisGraph.ZAxis.Max = 100;
		thisGraph.ZAxis.TicStep = 20;

		const int thisChonk = 25;
		thisGraph.XAxis.Chonk = thisChonk;
		thisGraph.YAxis.Chonk = thisChonk;
		thisGraph.ZAxis.Chonk = thisChonk;
		thisGraph.Transition();

		var periodicPlotter = new PeriodicPlotter();
		thisGraph.AddChild(periodicPlotter);
		periodicPlotter.graph = thisGraph;
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
		
		// Deleterious mutation bar plot
		// var barPlot3D = thisGraph.AddBarPlot3D();
		// barPlot3D.DataFetchMethod = BarData3DUtilities.PropertyHistogram2D(
		// 	() => CreatureSim.Registry.Entities,
		// 	creature =>
		// 	{
		// 		var deleteriousMutationProperties = new List<(float, float)>();
		// 		foreach (var trait in creature.Genome.Traits.Values)
		// 		{
		// 			if (trait is DeleteriousTrait deleteriousTrait && deleteriousTrait.Alleles.Any(x => x)) // Deleterious trait is true
		// 			{
		// 				deleteriousMutationProperties.Add((deleteriousTrait.ActivationAge, deleteriousTrait.MortalityRate * 100));
		// 			}
		// 		}
		//
		// 		return deleteriousMutationProperties;
		// 	},
		// 	new Histogram2DOptions{AdjustmentMethod = Histogram2DOptions.AdjustmentMethodType.PerCapita}
		// );
		//
		// periodicPlotter.BarPlot3D = barPlot3D;

		var barPlot = thisGraph.AddBarPlot();
		// barPlot.DataFetchMethod = BarDataUtilities.NormalizedPropertyHistogram(
		// 	() => CreatureSim.Registry.Entities,
		// 	creature =>
		// 	{
		// 		var alleles = creature.Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed").Alleles;
		// 		return alleles.Select(x => x ? 20f : 10f);
		// 	}
		// );
		barPlot.DataFetchMethod = BarDataUtilities.NormalizedPropertyHistogram(
			() => SimulationWorld.CreatureSim.Registry.Entities,
			creature =>
			{
				var alleles = creature.Genome.GetTrait<float>("MaxAge").Alleles;
				return alleles.Select(x =>
					{
						if (x <= 20) return 10f;
						if (x <= 40) return 20f;
						return 30f;
					} // Hacky custom binning to the float.MaxValue "no max" doesn't mess up the histogram
				);
			}
		);
		
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
		thisGraph2.AddChild(periodicPlotter2);
		periodicPlotter2.graph = thisGraph2;
		periodicPlotter2.Name = "Periodic plotter2";
		
		// Bar plot
		var barPlot2 = thisGraph2.AddBarPlot();
		// Max age histogram
		// barPlot.DataFetchMethod = BarDataUtilities.NormalizedPropertyHistogram(() => CreatureSim.Registry.Entities, x => x.MaxAge);
		// TODO: Get this to actually count alleles. Could allow it to take delegate returning an array of floats instead of just a single float
		// barPlot.DataFetchMethod = BarDataUtilities.NormalizedPropertyHistogram(() => CreatureSim.Registry.Entities, x => x.Genome.GetTrait<float>("MaxAge").Alleles /*...*/);
		
		// Cumulative death/survival plotting
		var creatureDeathAges = new List<float>();
		CreatureSim.CreatureDeathEvent += index =>
		{
			var age = SimulationWorld.CreatureSim.Registry.Entities[index].Age;
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
		thisGraph3.AddChild(periodicPlotter3);
		periodicPlotter3.graph = thisGraph3;
		periodicPlotter3.Name = "Periodic plotter3";
		
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
		
		CreatureSimSettings.Instance.FindMate = MateSelectionStrategies.FindFirstAvailableMate;
		CreatureSimSettings.Instance.Reproduce = ReproductionStrategies.SexualReproduce;
		CreatureSimSettings.Instance.InitializePopulation =
			InitialPopulationGeneration.WorkingInitialPopulationThatChangesALot;
		var creatureSim = new CreatureSim(SimulationWorld)
		{
			InitialEntityCount = _initialCreatureCount
		};

		var fruitTreeSimSettings = new FruitTreeSimSettings();
		var fruitTreeSim = new FruitTreeSim(SimulationWorld, fruitTreeSimSettings)
		{
			Mode = FruitTreeSim.SimMode.TreeGrowth,
			InitialEntityCount = _initialTreeCount
		};

		SimulationWorld.Initialize(creatureSim, fruitTreeSim);
		SimulationWorld.Running = true;
		fruitTreeSim.Initialize();

		const ulong treeGrowthStepGoal = 500;
		
		while (Engine.GetPhysicsFrames() < startPhysicsFrame + treeGrowthStepGoal) await Task.Delay(100);
		fruitTreeSim.SaveTreeDistribution();
		while (!_running) await Task.Delay(100);
		
		SimulationWorld.TimeScale = originalTimeScale;

		fruitTreeSim.Mode = FruitTreeSim.SimMode.FruitGrowth;

		await Task.Delay(3000);
		
		
		creatureSim.Initialize();
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
