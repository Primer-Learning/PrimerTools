using Godot;
using PrimerTools.Simulation;
using PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;
using PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;
using PrimerTools.Simulation.Visual;

namespace GladiatorManager.ContinuousSpaceTimeSims.CreatureSim.Visual;

public class CreatureVisualEventManager : IVisualEventManager
{
    private readonly VisualEntityRegistry _visualRegistry;

    public CreatureVisualEventManager(VisualEntityRegistry visualRegistry)
    {
        _visualRegistry = visualRegistry;
        _visualRegistry.RegisterEntityType(new DefaultCreatureFactory());
        _visualRegistry.SubscribeToComponentEvents<CreatureComponent, CreatureVisualEntity>();

        CreatureSystem.CreatureDeathEvent += OnCreatureDeath;
        CreatureSystem.CreatureEatEvent += OnCreatureEat;
    }

    private void OnCreatureDeath(EntityId entityId, CreatureSystem.DeathCause cause)
    {
        _visualRegistry.GetVisualEntity<CreatureVisualEntity>(entityId).HandleDeath(cause);
    }

    private void OnCreatureEat(EntityId creatureId, EntityId treeId, float duration)
    {
        var creature = _visualRegistry.GetVisualEntity<CreatureVisualEntity>(creatureId);
        var tree = _visualRegistry.GetVisualEntity<TreeVisualEntity>(treeId);
        creature.HandleEat(tree.GetFruit(), duration);
    }

    public void Cleanup()
    {
        CreatureSystem.CreatureDeathEvent -= OnCreatureDeath;
        CreatureSystem.CreatureEatEvent -= OnCreatureEat;
    }
}