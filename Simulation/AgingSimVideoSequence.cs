using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools;
using PrimerTools.Simulation;
using PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public abstract partial class AgingSimVideoSequence : AnimationSequence
{
    protected virtual void Prepare()
    {
        NextSimulationTimeToPlot = 0;
        if (Engine.IsEditorHint() && DataMode != SimulationDataManager.DataMode.Load)
        {
            GD.Print($"Editor context must load sim data. Temporarily using Load mode instead of {DataMode} mode.");
        }
        else
        {
            GD.Print($"Data Mode: {EffectiveDataMode}");
        }
        
        DataManager = new SimulationDataManager(DeathAgesDataPath, EffectiveDataMode, SaveInterval);
        
        if (EffectiveDataMode != SimulationDataManager.DataMode.Load)
        {
            // Set up death data collection
            // CreatureSim.CreatureDeathEvent += (entityID, cause) =>
            // {
            //     // var age = CreatureSim.Registry.Entities[index].Age;
            //     var age = CreatureSim.Registry.GetEntityById(entityID).Age;
            //     DataManager.RecordDeath(age, cause);
            // };
            //
            // // Set up data recording
            // if (EffectiveDataMode == SimulationDataManager.DataMode.Save)
            // {
            //     CreatureSim.Stepped += (stepCount) =>
            //     {
            //         var elapsedSimTime = stepCount * SimulationWorld.TimeStep;
            //         DataManager.RecordDataIfItIsTime(elapsedSimTime);
            //     };
            // }
        }
    }

	#region Sims
    protected SimulationWorld SimulationWorld;
    // protected CreatureSim CreatureSim;
    // protected FruitTreeSim FruitTreeSim;
    
    protected float SimTimeScale = 1;
    [Export] private VisualizationMode _visMode = VisualizationMode.None;
    protected void MakeSimulationWorld(int seed, Vector2? dimensions = null, Vector3? position = null)
    {
	    dimensions ??= new Vector2(100, 50);
	    
        SimulationWorld = new SimulationWorld();
        SimulationWorld.Seed = seed;
        SimulationWorld.WorldDimensions = dimensions.Value;
        AddChild(SimulationWorld);
        SimulationWorld.Position = position ?? new Vector3(0f, 0, 30);
        SimulationWorld.Name = "SimulationWorld";
        SimulationWorld.VisualizationMode = _visMode;
        SimulationWorld.TimeScaleControl = SimTimeScale;
    }

    protected void CreateSims(
	    int initialCreatureCount, 
	    InitialPopulationGeneratorDelegate populationGeneratorDelegate,
	    string fileName = "LifeExpectancyWorkingTreeDistribution.json"
	    )
    {
	    // CreatureSimSettings.Instance.FindMate = MateSelectionStrategies.FindFirstAvailableMate;
	    // CreatureSimSettings.Instance.Reproduce = ReproductionStrategies.SexualReproduce;
	    // CreatureSimSettings.Instance.InitializePopulation = populationGeneratorDelegate;
	    // CreatureSim = new CreatureSim(SimulationWorld);
	    // CreatureSim.InitialEntityCount = initialCreatureCount;
		
	    var fruitTreeSimSettings = new FruitTreeSimSettings();
	    // fruitTreeSimSettings.TreeDistributionPath =
		    // "addons/PrimerTools/Simulation/ContinuousSpaceTimeSims/TreeSim/Saved Tree Distributions/" + fileName;
	    // FruitTreeSim = new FruitTreeSim(SimulationWorld, fruitTreeSimSettings);
	    // FruitTreeSim.Mode = FruitTreeSim.SimMode.TreeGrowth;
	    // SimulationWorld.Initialize(CreatureSim, FruitTreeSim);
    }

    protected List<Vector3> InitialBlobLocations;
    protected void InitializeCreatureSim()
    {
	    // if (CreatureSim == null)
	    // {
		   //  GD.PushWarning("No creature sim to initialize.");
		   //  return;
	    // }
	    // CreatureSim.Initialize(true, InitialBlobLocations);
	    // FruitTreeSimSettings.FruitGrowthTime = 6;
	    // FruitTreeSimSettings.NodeFruitGrowthDelay = 2;
    }
    
    protected Animation MakeTreeAppearanceAnimation()
    {
        // if (SimulationWorld.VisualizationMode == VisualizationMode.None)
        // {
        GD.PushWarning("MakeTreeAppearanceAnimation hasn't been updated");
            return new Animation().WithDuration(0.5);
        // }
        // return SimulationWorld.GetNode<TreeVisualSystem>("NodeTreeManager")
        //     .AnimateGrowingAllTrees(0.5f);
    }
    #endregion

    #region Status printing
    private int _numTimePrints;
    protected Func<string> StatusStringGenerator;
    [Export] protected int TimePrintInterval;
    public override void _Ready()
    {
	    base._Ready();
	    _numTimePrints = 0;
    }

    private bool _saved;
    public override void _Process(double delta)
    {
        // Saving creature death ages over time
        if (!Engine.IsEditorHint() && !_saved && EffectiveDataMode == SimulationDataManager.DataMode.Save && SimulationWorld.PhysicsStepsTaken > 15000)
        {
            DataManager.SaveAllSimulationData();
            _saved = true;
        }

        if (!Engine.IsEditorHint() && TimePrintInterval > 0)
        {
            // Sim time monitoring
            if (StatusStringGenerator != null && SimulationWorld.TimeElapsed > (_numTimePrints + 1) * TimePrintInterval)
            {
                GD.Print(StatusStringGenerator());
                _numTimePrints++;
            }
        }
    }
    #endregion
    
    #region Plot tweening
    private int _simPlotTime = -1;

    public int SimPlotTime
    {
        get => _simPlotTime;
        set
        {
            var oldValue = _simPlotTime;
            _simPlotTime = value;
            // GD.Print($"Old value: {oldValue}, New value: {value}");
            if (value == -1 || value == oldValue) return;
            if (!DoneWithConstruction) return;
            // GD.Print($"Triggering plot method with value {value}");
            GeneralPlotUpdateMethod(value);
        }
    }
    
    // For converting scene time to simulation time
    // Could be automatically tracked somehow. Easy to forget to set it properly.
    protected double SceneTimePassedWithSimNotRunning;
    protected double NextSimulationTimeToPlot; // To make it easier to pick up where you left off in a scene
    protected void RegisterPlotUpdateAnimations(
        double sceneStartTime, 
        double sceneEndTime, 
        double scenePlotPeriod, 
        double simulationStartTime,
        int indexOfPlaybackTrack = 1)
    {
        if (scenePlotPeriod <= 0)
        {
            GD.PushWarning("Plot update period is not positive. The method would never end, so not starting it.");
            return;
        }

        NextSimulationTimeToPlot = simulationStartTime;
        
        while (sceneStartTime < sceneEndTime)
        {
            RegisterPlotUpdateAnimation(sceneStartTime, indexOfPlaybackTrack);
            sceneStartTime += scenePlotPeriod;
        }
    }

    protected void RegisterPlotUpdateAnimation(double time, int indexOfPlaybackTrack = 1, double duration = 0.5f)
    {
        RegisterAnimation(
            GeneralPlotUpdateMethod((float) NextSimulationTimeToPlot).WithDuration(duration),
            time: time,
            indexOfPlaybackTrack: indexOfPlaybackTrack
        );
        NextSimulationTimeToPlot += SimTimeScale;
    }
    // private async void PeriodicallyPlot(double start, double end, double period)
    // {
    //     while (Time.GetTicksMsec() / 1000f < start)
    //     {
    //         await Task.Delay(100);
    //     }
    //     
    //     while (Time.GetTicksMsec() / 1000f < end)
    //     {
    //         var timeSimHasBeenRunningInTheScene = Time.GetTicksMsec() / 1000f - SceneTimePassedWithSimNotRunning;
    //         PlotTimeTarget = (float)timeSimHasBeenRunningInTheScene * SimTimeScale;
    //         // GeneralPlotUpdateMethod();
    //         await Task.Delay((int)(period * 1000));
    //     }
    // }
    // protected float PlotTimeTarget;

    protected abstract Animation GeneralPlotUpdateMethod(float simTime);
    
    #endregion

    #region Simulation Data saving and loading
    
    protected SimulationDataManager DataManager;
    
    [Export]
    protected SimulationDataManager.DataMode DataMode = SimulationDataManager.DataMode.None;

    protected SimulationDataManager.DataMode EffectiveDataMode => //DataMode;
        Engine.IsEditorHint() ? SimulationDataManager.DataMode.Load : DataMode;
    protected string DeathAgesDataPath;
    protected float SaveInterval = 5f;

    protected virtual void SaveCustomData(int stepCount) { }

    protected float[] GetDeathAgeHistogramAtSimTime(float simTime)
    {
        var histograms = DataManager?.GetDeathAgeHistogramsByCausesAtSimulationTime(simTime);
        if (histograms == null) return Array.Empty<float>();

        // Find the maximum length among all histograms
        var maxLength = histograms.Max(h => h.Length);
        if (maxLength == 0) return Array.Empty<float>();

        // Initialize combined histogram with zeros
        var combinedHistogram = new float[maxLength];

        // Sum each histogram into the combined histogram
        foreach (var histogram in histograms)
        {
            for (var i = 0; i < histogram.Length; i++)
            {
                combinedHistogram[i] += histogram[i];
            }
        }

        return combinedHistogram;
    }
    
    protected float[][] GetDeathAgeHistogramsByCauseAtSimTime(float simTime)
    {
        return DataManager?.GetDeathAgeHistogramsByCausesAtSimulationTime(simTime) ?? Array.Empty<float[]>();
    }
    
    #endregion
}
