using Godot;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimerTools.Graph;
using PrimerTools.Simulation;

[Tool]
public partial class SimulationTestScene : Node3D
{
	private SimulationWorld SimulationWorld => GetNode<SimulationWorld>("SimulationWorld");
	private CreatureSim CreatureSim => SimulationWorld.GetNode<CreatureSim>("Creature Sim");
	private FruitTreeSim FruitTreeSim => SimulationWorld.GetNode<FruitTreeSim>("Tree Sim");
	
	private CancellationTokenSource _cts;
	private bool _newSim = true;
	private bool _run;
	[Export] private bool Run {
		get => _run;
		set {
			if (value)
			{
				if (_newSim)
				{
					GD.Print("Running new sim");
					CreatePlot();
					_cts = new();
					RunSimSequence(_cts.Token);
					_newSim = false;
				}
				else
				{
					GD.Print("Continuing sim");
					SimulationWorld.Running = true;
					if (PeriodicPlotter != null) PeriodicPlotter.Plotting = true;
				}
			}
			else if (_run)
			{
				GD.Print("Pausing");
				_cts?.Cancel();
				SimulationWorld.Running = false;
				if (PeriodicPlotter != null) PeriodicPlotter.Plotting = false;
				SimulationWorld.TimeScale = 1;
				
				// These prevent continuation of the sim without resetting.
				// But setting these true on run would mess up the start sequence.
				// If continuing becomes important, a separate pause button might do it.
				CreatureSim.Running = false;
				FruitTreeSim.Running = false;
			}
			_run = value;
		}
	}
	
	private bool _reset;
	[Export] private bool Reset {
		get => _reset;
		set {
			if (value && !_reset)
			{
				GD.Print("Resetting");
				SimulationWorld.ResetSimulations();
				SimulationWorld.Running = false;
				CreatureSim.Running = false;
				FruitTreeSim.Running = false;
				
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

	private bool _speedTest;
	[Export] private bool SpeedTest
	{
		get => _speedTest;
		set
		{
			if (value)
			{
				SimulationWorld.TimeScale = 1000;
				SimulationWorld.VisualizationMode = VisualizationMode.None;
                SimulationWorld.Seed = 0;
			}
			else
			{
				SimulationWorld.TimeScale = 1;
				SimulationWorld.VisualizationMode = VisualizationMode.NodeCreatures;
                SimulationWorld.Seed = -1;
			}

			_speedTest = value;
		}
	}

	private bool _performanceTest;
	
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

	private async Task RunSimSequence(CancellationToken ct = default)
	{
		// try
		// {
			var originalTimeScale = SimulationWorld.TimeScale;
			SimulationWorld.TimeScale = 99999;
			SimulationWorld.Initialize();
			SimulationWorld.Running = true;

			FruitTreeSim.Mode = FruitTreeSim.SimMode.TreeGrowth;
			FruitTreeSim.Running = true;

			CreatureSim.Initialize();

			await Task.Delay(2000, ct);
			ct.ThrowIfCancellationRequested();
			SimulationWorld.TimeScale = originalTimeScale;

			FruitTreeSim.Mode = FruitTreeSim.SimMode.FruitGrowth;

			await Task.Delay(3000, ct);
			ct.ThrowIfCancellationRequested();

			CreatureSim.Running = true;
			if (PeriodicPlotter != null) PeriodicPlotter.Plotting = true;
		// }
		// catch
		// {
		// 	GD.Print("Canceled");
		// }
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint()) return;
		Run = true;
		Engine.MaxFps = 0;
	}
}
