using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using MemoryPack;
using PrimerTools.Graph;
using PrimerTools.Simulation;
using PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public class SimulationDataManager
{
    // TODO: After aging video is done, generalize this to not have a built-in death age histogram and to
    // accept an arbitrary number custom histograms with generic data types.
    // ACTUALLY, it might be better to design this so each SimulationDataManager
    // handles one type of data. For example, in the aging video, I could have made
    // one version of this that handled death age data saving and loading and also 
    // defined the methods for organizing it into the absolute, relative, and cumulative
    // plots. And then each scene would need an additional allele tracking SimulationDataManager.
    
    private readonly string _basePath;
    // TODO: Save the save interval to the data file, to prevent confusion if it's changed. Could be in the name.
    private readonly float _saveInterval;
    private float _lastSaveTime;
    
    // Core death age data
    private List<(float age, DataCreature.DeathCause cause)> _deathAges;
    private List<List<float[]>> _deathAgeHistograms;
    private float[][][] _cachedLoadedDeathAgeHistograms;
    
    // Custom 1D histogram data
    private List<float[]> _customHistograms;
    private float[][] _cachedCustomHistograms;
    public int CustomHistogramCount => _cachedCustomHistograms.Length;
    private Func<float[]> _customHistogramGenerator;
    
    // Custom 2D histogram data
    private List<float[,]> _customHistograms2D;
    private float[][,] _cachedCustomHistograms2D;
    public int CustomHistogram2DCount => _cachedCustomHistograms2D.Length;
    private Func<float[,]> _customHistogram2DGenerator;
    
    public enum DataMode { None, Save, Load }
    public DataMode CurrentMode { get; }

    public SimulationDataManager(string basePath, DataMode mode, float saveInterval = 5f)
    {
        _basePath = basePath;
        _saveInterval = saveInterval;
        _lastSaveTime = -saveInterval; // So the first recorded moment will be the initial state. HAX.
        CurrentMode = mode;
        
        if (mode != DataMode.Load)
        {
            _deathAges = new List<(float, DataCreature.DeathCause)>();
            _deathAgeHistograms = new List<List<float[]>>();
            _customHistograms = new List<float[]>();
            _customHistograms2D = new List<float[,]>();
        }
        else if (mode == DataMode.Load)
        {
            GD.Print("Loadinggg");
            LoadAllSimulationData();
        }
    }

    /// <summary>
    /// Add deaths to death ages histogram. This gets subscribed to the creature sim's death event.
    /// Won't belong in the eventual generalized version of this class.
    /// </summary>
    /// <param name="age"></param>
    /// <param name="cause"></param>
    public void RecordDeath(float age, DataCreature.DeathCause cause)
    {
        if (CurrentMode == DataMode.Load) return;
        _deathAges.Add((age, cause));
        if (_deathAges.Count > 10000) _deathAges.RemoveAt(0);
    }
    public void RecordDataIfItIsTime(float currentSimTime)
    {
        if (CurrentMode != DataMode.Save) return;
        if (currentSimTime - _lastSaveTime >= _saveInterval)
        {
            // Record death age histograms
            var histogramsByCause = new List<float[]>();
            foreach (var cause in Enum.GetValues(typeof(DataCreature.DeathCause)))
            {
                var causeHistogram = BarDataUtilities.MakeHistogram(
                    _deathAges
                        .Where(x => x.Item2 == (DataCreature.DeathCause)cause)
                        .Select(x => x.Item1)
                ).ToArray();
                histogramsByCause.Add(causeHistogram);
            }
            _deathAgeHistograms.Add(histogramsByCause);

            // Record custom histograms if generators exist
            if (_customHistogramGenerator != null)
            {
                _customHistograms.Add(_customHistogramGenerator());
            }
            if (_customHistogram2DGenerator != null)
            {
                _customHistograms2D.Add(_customHistogram2DGenerator());
            }
            _lastSaveTime += _saveInterval;
        }
    }
    
    // The main reason for this is so this class knows how so save the custom data
    // It's also used by GeneralPlotUpdate methods in AgingSimVideoSequence inheritors.
    // Maybe GeneralPlotUpdateMethod could go in this class?
    // Though that would mean this class has to know how to turn the data into a plot,
    // but its current purpose is to just save and load the data.
    public void SetCustomHistogramGenerator(Func<float[]> generator)
    {
        _customHistogramGenerator = generator;
    }
    
    public void SetCustomHistogram2DGenerator(Func<float[,]> generator)
    {
        _customHistogram2DGenerator = generator;
    }
    public float[] GetCustomHistogramAtSimulationTime(float simulationTime)
    {
        if (CurrentMode == DataMode.Load)
        {
            if (_cachedCustomHistograms == null) return Array.Empty<float>();
            var dataIndex = Mathf.FloorToInt(simulationTime / _saveInterval);
            return _cachedCustomHistograms[
                Mathf.Max(Mathf.Min(dataIndex, _cachedCustomHistograms.Length - 1), 0)
            ];
        }

        // For save mode, return current custom histogram
        return _customHistogramGenerator?.Invoke() ?? Array.Empty<float>();
    }
    
    public float[,] GetCustomHistogram2DAtSimulationTime(float simulationTime)
    {
        if (CurrentMode == DataMode.Load)
        {
            if (_cachedCustomHistograms2D == null) return new float[0,0];
            var dataIndex = Mathf.FloorToInt(simulationTime / _saveInterval);
            return _cachedCustomHistograms2D[
                Mathf.Max(Mathf.Min(dataIndex, _cachedCustomHistograms2D.Length - 1), 0)
            ];
        }

        // For save mode, return current custom 2D histogram
        return _customHistogram2DGenerator?.Invoke() ?? new float[0,0];
    }
    public float[][] GetDeathAgeHistogramsByCausesAtSimulationTime(float simulationTime)
    {
        
        if (CurrentMode == DataMode.Load)
        {
            var dataIndex = Mathf.FloorToInt(simulationTime / _saveInterval);
            return _cachedLoadedDeathAgeHistograms[
                Mathf.Max(Mathf.Min(dataIndex, _cachedLoadedDeathAgeHistograms.Length - 1), 0)
            ];
        }
        // For Save or None mode, return current histograms
        var histogramsByCause = new List<float[]>();
        foreach (var cause in Enum.GetValues(typeof(DataCreature.DeathCause)))
        {
            histogramsByCause.Add(BarDataUtilities.MakeHistogram(
                _deathAges
                    .Where(x => x.Item2 == (DataCreature.DeathCause)cause)
                    .Select(x => x.Item1)
            ).ToArray());
        }
        return histogramsByCause.ToArray();
    }

    public void SaveAllSimulationData()
    {
        if (CurrentMode != DataMode.Save)
        {
            GD.PushWarning("Tried to save, but I'm not in save mode. I haven't been tracking the data you want to save to disk.");
            return;
        }
        
        // Save death age data
        var deathAgeData = _deathAgeHistograms.Select(list => list.ToArray()).ToArray();
        SaveSimulationData($"{_basePath}_DeathAges.bin", deathAgeData);
        
        // Save custom data if it exists
        if (_customHistograms.Any())
        {
            SaveSimulationData($"{_basePath}_CustomData.bin", _customHistograms.ToArray());
        }
        if (_customHistograms2D.Any())
        {
            SaveSimulationData($"{_basePath}_CustomData2D.bin", _customHistograms2D.ToArray());
        }
    }
    private void LoadAllSimulationData()
    {
        GD.Print("Loading simulation data");
        if (string.IsNullOrEmpty(_basePath))
        {
            GD.Print("No data path provided for loading");
            return;
        }
        _cachedLoadedDeathAgeHistograms = LoadSimulationData<float[][][]>($"{_basePath}_DeathAges.bin");
        
        try
        {
            _cachedCustomHistograms = LoadSimulationData<float[][]>($"{_basePath}_CustomData.bin");
        }
        catch (FileNotFoundException)
        {
            GD.Print("No custom 1D data file found - this is normal if the scene doesn't use custom 1D data");
            _cachedCustomHistograms = null;
        }
        
        GD.Print("Loading simulation data 2");
        
        try
        {
            _cachedCustomHistograms2D = LoadSimulationData<float[][,]>($"{_basePath}_CustomData2D.bin");
        }
        catch (FileNotFoundException)
        {
            GD.Print("No custom 2D data file found - this is normal if the scene doesn't use custom 2D data");
            _cachedCustomHistograms2D = null;
        }
    }
    private void SaveSimulationData<T>(string path, T data)
    {
        var finalPath = ProjectSettings.GlobalizePath(path);
        if (!finalPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            finalPath += ".bin";
        var directoryName = Path.GetDirectoryName(finalPath);
        if (directoryName == null)
        {
            GD.Print($"Failed to get directory name for path: {finalPath}");
            return;
        }
        Directory.CreateDirectory(directoryName);
        
        var bytes = MemoryPackSerializer.Serialize(data);
        File.WriteAllBytes(finalPath, bytes);
        GD.Print($"Saved data to {finalPath}");
    }
    private T LoadSimulationData<T>(string path)
    {
        var finalPath = ProjectSettings.GlobalizePath(path);
        if (!File.Exists(finalPath))
            throw new FileNotFoundException($"Data file not found at {finalPath}");
            
        var bytes = File.ReadAllBytes(finalPath);
        var loadedData = MemoryPackSerializer.Deserialize<T>(bytes);
        GD.Print($"Loaded data from {finalPath}");
        return loadedData;
    }
}
