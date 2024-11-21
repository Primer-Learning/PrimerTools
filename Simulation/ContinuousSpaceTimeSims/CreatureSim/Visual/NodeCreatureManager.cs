using System.Linq;
using Godot;

namespace PrimerTools.Simulation.New;

public partial class NodeCreatureManager : NodeEntityManager<DataCreature, NodeCreature>
{
    public NodeCreatureManager(DataEntityRegistry<DataCreature> dataEntityRegistry) 
        : base(dataEntityRegistry)
    {
        CreatureSim.CreatureEatEvent += OnCreatureEat;
        CreatureSim.CreatureDeathEvent += OnCreatureDeath;
    }
    public NodeCreatureManager(){}

    private NodeTreeManager _nodeTreeManager;

    private NodeTreeManager NodeTreeManager
    {
        get
        {
            if (_nodeTreeManager == null)
            {
                _nodeTreeManager = GetParent().GetChildren().OfType<NodeTreeManager>().FirstOrDefault();
            }
            return _nodeTreeManager;
        }
    }

    private void OnCreatureEat(int creatureIndex, Rid treeID, float duration)
    {
        NodeEntities[creatureIndex].Eat(
            NodeTreeManager?.GetNodeEntityByDataID(treeID)?.GetFruit(),
            duration
        );
    }

    private void OnCreatureDeath(int creatureIndex, CreatureSim.DeathCause cause)
    {
        NodeEntities[creatureIndex].Death();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CreatureSim.CreatureEatEvent -= OnCreatureEat;
        CreatureSim.CreatureDeathEvent -= OnCreatureDeath;
    }
}
