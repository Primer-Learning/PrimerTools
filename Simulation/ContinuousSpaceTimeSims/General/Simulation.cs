using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public abstract class Simulation<TDataEntity> : ISimulation
    where TDataEntity : IDataEntity
{
    public int InitialEntityCount = 2;
    
    protected Simulation(SimulationWorld simulationWorld)
    {
        SimulationWorld = simulationWorld;
        Registry = new DataEntityRegistry<TDataEntity>(simulationWorld.World3D);
    }

    #region Simulation
    protected readonly SimulationWorld SimulationWorld;
    public readonly DataEntityRegistry<TDataEntity> Registry;

    private bool _initialized;
    private bool _running;

    public void Initialize(bool run = true, IEnumerable<Vector3> initialPositions = null)
    {
        if (_initialized) return;
        _initialized = true;
        _running = run;
        
        CustomInitialize(initialPositions);
    }

    public void Start()
    {
        _running = true;
    }
    
    protected abstract void CustomInitialize(IEnumerable<Vector3> initialPositions);
    public virtual void Reset()
    {
        Registry?.Reset();
        _initialized = false;
        _running = false;
    }
    
    public void Step()
    {
        if (!_running) return;
        if (Registry.Entities.Count == 0)
        {
            GD.Print($"No {typeof(TDataEntity)}s found. Stopping.");
            _running = false;
            return;
        }
        CustomStep();
    }

    public void ClearDeadEntities()
    {
        Registry.ClearDeadEntities();
    }

    protected abstract void CustomStep();
    #endregion
}
