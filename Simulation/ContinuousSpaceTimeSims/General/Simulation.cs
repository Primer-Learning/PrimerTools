using Godot;

namespace PrimerTools.Simulation;

public abstract class Simulation<TDataEntity> : ISimulation
    where TDataEntity : IDataEntity
{
    public int InitialEntityCount = 2;
    
    protected Simulation(SimulationWorld simulationWorld)
    {
        this.simulationWorld = simulationWorld;
        Registry = new DataEntityRegistry<TDataEntity>(simulationWorld.World3D);
    }

    #region Simulation
    protected readonly SimulationWorld simulationWorld;
    public readonly DataEntityRegistry<TDataEntity> Registry;

    private bool _initialized;
    private bool _running;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _running = true;
        
        CustomInitialize();
    }
    protected abstract void CustomInitialize();
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
