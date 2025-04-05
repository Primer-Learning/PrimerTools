using Godot;
using PrimerTools.Simulation;
using PrimerTools.Simulation.Components;
using PrimerTools.Simulation.Visual;

namespace GladiatorManager.ContinuousSpaceTimeSims.CreatureSim.Visual;

public class TreeVisualEventManager : IVisualEventManager
{
    private readonly VisualEntityRegistry _visualRegistry;

    public TreeVisualEventManager(VisualEntityRegistry visualRegistry)
    {
        _visualRegistry = visualRegistry;
        _visualRegistry.RegisterEntityType<TreeVisualEntity>();
        _visualRegistry.SubscribeToComponentEvents<TreeComponent, TreeVisualEntity>();

        TreeSystem.TreeDeathEvent += OnTreeDeath;
    }
    
    private void OnTreeDeath(EntityId entityId)
    {
        _visualRegistry.GetVisualEntity<TreeVisualEntity>(entityId).Death();
    }

    public void Cleanup()
    {
        TreeSystem.TreeDeathEvent -= OnTreeDeath;
    }
}