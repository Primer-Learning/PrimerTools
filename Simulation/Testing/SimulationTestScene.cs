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
					if (PeriodicPlotter != null) PeriodicPlotter.Plotting = true;
				}
			}
			else if (_running)
			{
				GD.Print("Pausing");
				SimulationWorld.Running = false;
				if (PeriodicPlotter != null) PeriodicPlotter.Plotting = false;
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
	
	private void CreatePlot()
	{
		foreach (var child in GraphParent.GetChildren())
		{
			if (IsInstanceValid(child)) child.Free();
		}
		
		var thisGraph = Graph.CreateInstance();
		GraphParent.AddChild(thisGraph);
		// thisGraph.Owner = GetTree().EditedSceneRoot;
		thisGraph.XAxis.length = 60;
		thisGraph.XAxis.Max = 40;
		thisGraph.XAxis.TicStep = 20;
		
		thisGraph.YAxis.length = 30;
		thisGraph.YAxis.Max = 50;
		thisGraph.YAxis.TicStep = 10;
		
		thisGraph.ZAxis.length = 0;

		var thisChonk = 25;
		thisGraph.XAxis.Chonk = thisChonk;
		thisGraph.YAxis.Chonk = thisChonk;
		thisGraph.ZAxis.Chonk = thisChonk;
		thisGraph.Transition();

		var curve = thisGraph.AddCurvePlot2D();
		curve.Width = 200;

		curve.DataFetchMethod = () =>
		{
			var dataList = curve.GetData().ToList();
			
			// Creature count
			// dataList.Add( new Vector3(dataList.Count, CreatureSim.Registry.Entities.Count(x => x.Alive), 0) );
			
			// Average age
			dataList.Add( new Vector3(dataList.Count, CreatureSim.Registry.Entities.Average(x => x.Age), 0) );
			
			return dataList;
		};
		
		var periodicPlotter = new PeriodicPlotter();
		GraphParent.AddChild(periodicPlotter);
		periodicPlotter.Name = "Periodic plotter";
		periodicPlotter.Curve = curve;
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

		const ulong treeGrowthStepGoal = 200;
		
		while (Engine.GetPhysicsFrames() < startPhysicsFrame + treeGrowthStepGoal) await Task.Delay(100);
		while (!_running) await Task.Delay(100);
		
		SimulationWorld.TimeScale = originalTimeScale;

		FruitTreeSim.Mode = FruitTreeSim.SimMode.FruitGrowth;

		await Task.Delay(3000);

		CreatureSim.InitialEntityCount = _initialCreatureCount;
		CreatureSim.Initialize();
		if (PeriodicPlotter != null) PeriodicPlotter.Plotting = true;
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint()) return;
		Run = true;
		Engine.MaxFps = 0;
	}
}
