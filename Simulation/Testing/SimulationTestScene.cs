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
					CreatePlot();
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
				SimulationWorld.TimeScale = 1;
			}
			_reset = false;
		}
	}

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
		thisGraph.YAxis.Max = 100;
		thisGraph.YAxis.TicStep = 20;
		
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
			dataList.Add( new Vector3(dataList.Count, CreatureSim.Registry.Entities.Count(x => ((DataCreature)x).Alive), 0) );
			return dataList;
		};
		
		var periodicPlotter = new PeriodicPlotter();
		GraphParent.AddChild(periodicPlotter);
		periodicPlotter.Name = "Periodic plotter";
		periodicPlotter.Curve = curve;
	}

	private async Task RunSimSequence()
	{
		var originalTimeScale = SimulationWorld.TimeScale;
		SimulationWorld.TimeScale = 99999;
		SimulationWorld.Initialize();
		SimulationWorld.Running = true;
		
		FruitTreeSim.Mode = FruitTreeSim.SimMode.TreeGrowth;
		FruitTreeSim.InitialEntityCount = _initialTreeCount;
		FruitTreeSim.Initialize();
		
		await Task.Delay(3000);
		while (!_running) await Task.Delay(100);
		
		SimulationWorld.TimeScale = originalTimeScale;

		FruitTreeSim.Mode = FruitTreeSim.SimMode.FruitGrowth;

		await Task.Delay(3000);
		while (!_running) await Task.Delay(100);

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
