using Godot;

namespace PrimerTools.Simulation;

public abstract class Simulation<TDataEntity, TNodeEntity> : ISimulation
    where TDataEntity : IDataEntity
    where TNodeEntity : NodeEntity<TDataEntity>, new()
{
    public int InitialEntityCount = 2;
    
    protected Simulation(SimulationWorld simulationWorld)
    {
        SimulationWorld = simulationWorld;
    }

    #region Simulation
    protected readonly SimulationWorld SimulationWorld;
    public DataEntityRegistry<TDataEntity> Registry;
    public NodeEntityManager<TDataEntity, TNodeEntity> EntityManager;

    private bool _initialized;
    private bool _running;

    public Simulation()
    {
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _running = true;
        
        Registry = new DataEntityRegistry<TDataEntity>(SimulationWorld.World3D);
        
        CustomInitialize();
    }
    protected abstract void CustomInitialize();
    public virtual void Reset()
    {
        Registry?.Reset();
        EntityManager?.Reset();
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
    protected abstract void CustomStep();

    protected void RegisterEntity(TDataEntity dataEntity)
    {
        Registry.RegisterEntity(dataEntity);
    }
    
    public void ClearDeadEntities()
    {
        if (!_running) return;
        for (var i = Registry.Entities.Count - 1; i >= 0; i--)
        {
            if (Registry.Entities[i].Alive) continue;
			
            Registry.Entities[i].CleanUp();
            Registry.Entities.RemoveAt(i);
            
            if (EntityManager != null && EntityManager.NodeEntities.Count > 0)
            {
                // Visual creatures aren't cleaned up here, since they may want to do an animation before freeing the object
                // But we clear the list here so they stay in sync.
                // For this reason, NodeCreature.Death must handle cleanup.
                EntityManager.RemoveEntity(i);
            }
        }
		
        // Rebuild Lookup
        Registry.EntityLookup.Clear();
        for (int i = 0; i < Registry.Entities.Count; i++)
        {
            Registry.EntityLookup[Registry.Entities[i].Body] = i;
        }
    }
    #endregion
}
