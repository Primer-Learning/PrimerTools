using Godot;
using PrimerTools.Simulation;
using PrimerTools.Simulation.Components;
using PrimerTools.Simulation.Visual;

namespace GladiatorManager.ContinuousSpaceTimeSims.CreatureSim.Visual;

public class TreeVisualEventManager
{
    private readonly VisualEntityRegistry _visualRegistry;

    public TreeVisualEventManager(VisualEntityRegistry visualRegistry, Node parent)
    {
        _visualRegistry = visualRegistry;
        _visualRegistry.RegisterEntityType<TreeVisualEntity>();
        _visualRegistry.SubscribeToComponentEvents<TreeComponent, TreeVisualEntity>();

        TreeSystem.TreeDeathEvent += (entityId) => _visualRegistry.GetVisualEntity<TreeVisualEntity>(entityId).Death();
    }
}