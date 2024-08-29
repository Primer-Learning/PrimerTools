using Godot;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aging.addons.PrimerTools.Simulation.Aging;
using PrimerTools.Graph;
using PrimerTools.Simulation.Aging;

[Tool]
public partial class SimulationTestScene : Node3D
{
	private SimulationWorld SimulationWorld => GetNode<SimulationWorld>("SimulationWorld");
	private CreatureSim CreatureSim => SimulationWorld.GetNode<CreatureSim>("Creature Sim");
	private TreeSim TreeSim => SimulationWorld.GetNode<TreeSim>("Tree Sim");
	
	private CancellationTokenSource _cts;
	
	private bool _run;
	[Export] private bool Run {
		get => _run;
		set {
			if (value)
			{
				CreatePlot();
				_cts = new();
				RunSimSequence(_cts.Token);
			}
			else if (_run)
			{
				GD.Print("Stopping");
				_cts.Cancel();
				SimulationWorld.Running = false;
				CreatureSim.Running = false;
				SimulationWorld.Running = false;
				_periodicPlotter.Plotting = false;
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
				foreach (var child in GraphParent.GetChildren())
				{
					if (IsInstanceValid(child)) child.Free();
				}
			}
			_reset = false;
		}
	}
	
	// private CreatureSimEntityRegistry Registry => GetParent<Node3D>().GetNode<CreatureSim>("Creature Sim").Registry;
	private CurvePlot2D _curve;
	private Node3D GraphParent => GetNode<Node3D>("GraphParent");
	private PeriodicPlotter _periodicPlotter;
	
	private void CreatePlot()
	{
		var thisGraph = Graph.CreateInstance();
		GraphParent.AddChild(thisGraph);
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
			dataList.Add( new Vector3(dataList.Count, CreatureSim.Registry.PhysicalCreatures.Count(x => x.Alive), 0) );
			return dataList;
		};

		_periodicPlotter = new PeriodicPlotter();
		GraphParent.AddChild(_periodicPlotter);
		_periodicPlotter.Owner = GetTree().EditedSceneRoot;
		_periodicPlotter.Name = "Periodic plotter";
		_periodicPlotter.Curve = _curve;
	}

	private async Task RunSimSequence(CancellationToken ct = default)
	{
		try
		{
			SimulationWorld.TimeScale = 99999;
			SimulationWorld.Initialize();
			SimulationWorld.Running = true;

			TreeSim.Mode = TreeSim.SimMode.TreeGrowth;
			TreeSim.Running = true;

			await Task.Delay(5000, ct);
			ct.ThrowIfCancellationRequested();
			SimulationWorld.TimeScale = 1;

			TreeSim.Mode = TreeSim.SimMode.FruitGrowth;

			await Task.Delay(5000, ct);
			ct.ThrowIfCancellationRequested();

			CreatureSim.Running = true;
			_periodicPlotter.Plotting = true;
		}
		catch
		{
			GD.Print("Canceled");
		}
	} 
}
