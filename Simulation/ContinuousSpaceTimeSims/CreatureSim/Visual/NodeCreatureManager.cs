using System.Linq;
using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;

public partial class NodeCreatureManager : NodeEntityManager<DataCreature>
{
    private readonly ICreatureFactory _creatureFactory;

    public NodeCreatureManager(
        DataEntityRegistry<DataCreature> dataEntityRegistry,
        ICreatureFactory creatureFactory)
        : base(dataEntityRegistry, () => new NodeCreature(creatureFactory))
    {
        _creatureFactory = creatureFactory;
        CreatureSim.CreatureEatEvent += OnCreatureEat;
        CreatureSim.CreatureDeathEvent += OnCreatureDeath;
    }

    public NodeCreatureManager() : base(null, null) {}

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
        (NodeEntities[creatureIndex] as NodeCreature)?.Eat(
            NodeTreeManager?.GetNodeEntityByDataID(treeID)?.GetFruit(),
            duration
        );
    }

    private void OnCreatureDeath(int creatureIndex, CreatureSim.DeathCause cause)
    {
        (NodeEntities[creatureIndex] as NodeCreature)?.Death();
    }

    public override void _ExitTree()
    {
        CreatureSim.CreatureEatEvent -= OnCreatureEat;
        CreatureSim.CreatureDeathEvent -= OnCreatureDeath;
        base._ExitTree();
    }
}
