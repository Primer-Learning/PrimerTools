using System.Linq;
using Godot;

namespace PrimerTools.Simulation.New;

public partial class NodeCreatureManager : Node3D
{
    private readonly NodeEntityManager<DataCreature> _entityManager;
    private readonly ICreatureFactory _creatureFactory;

    public NodeCreatureManager(
        DataEntityRegistry<DataCreature> dataEntityRegistry,
        ICreatureFactory creatureFactory)
    {
        _creatureFactory = creatureFactory;
        _entityManager = new NodeEntityManager<DataCreature>(
            dataEntityRegistry,
            this,
            () => new NodeCreature(_creatureFactory));
            
        CreatureSim.CreatureEatEvent += OnCreatureEat;
        CreatureSim.CreatureDeathEvent += OnCreatureDeath;
    }
    public void VisualProcess(double delta) => _entityManager.VisualProcess(delta);
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
        (_entityManager.NodeEntities[creatureIndex] as NodeCreature)?.Eat(
            NodeTreeManager?.GetNodeEntityByDataID(treeID)?.GetFruit(),
            duration
        );
    }

    private void OnCreatureDeath(int creatureIndex, CreatureSim.DeathCause cause)
    {
        (_entityManager.NodeEntities[creatureIndex] as NodeCreature)?.Death();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CreatureSim.CreatureEatEvent -= OnCreatureEat;
        CreatureSim.CreatureDeathEvent -= OnCreatureDeath;
    }
}
