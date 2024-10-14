using Godot;

namespace PrimerTools.Simulation.New;

public partial class NodeCreatureManager : NodeEntityManager<DataCreature, NodeCreature>
{
    private NodeTreeManager _fruitTreeManager;

    public NodeCreatureManager(DataEntityRegistry<DataCreature> dataEntityRegistry, NodeTreeManager fruitTreeManager) 
        : base(dataEntityRegistry)
    {
        _fruitTreeManager = fruitTreeManager;
        CreatureSimSettings.CreatureEatEvent += OnCreatureEat;
        CreatureSim.CreatureDeathEvent += OnCreatureDeath;
    }

    private void OnCreatureEat(int creatureIndex, Rid treeID, float duration)
    {
        NodeEntities[creatureIndex].Eat(
            _fruitTreeManager.GetNodeEntityByDataID(treeID)?.GetFruit(),
            duration
        );
    }

    private void OnCreatureDeath(int creatureIndex)
    {
        NodeEntities[creatureIndex].Death();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CreatureSimSettings.CreatureEatEvent -= OnCreatureEat;
        CreatureSim.CreatureDeathEvent -= OnCreatureDeath;
    }
}
