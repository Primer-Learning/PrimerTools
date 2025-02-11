using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public partial class NodeTreeManager : Node3D
{
    private readonly NodeEntityManager<DataTree> _entityManager;

    public NodeTreeManager(DataEntityRegistry<DataTree> dataEntityRegistry)
    {
        _entityManager = new NodeEntityManager<DataTree>(
            dataEntityRegistry,
            this,
            () => new NodeTree());
    }

    public void VisualProcess(double delta) => _entityManager.VisualProcess(delta);
    
    public NodeTreeManager(){}
    
    public NodeTree GetNodeEntityByDataID(Rid rid) => _entityManager.GetNodeEntityByDataID<NodeTree>(rid);

    // TODO: Figure out a way to handle warnings like this now that we no longer inherit from NodeEntityManager
    // public override void VisualProcess(double delta)
    // {
    //     if (FruitTreeSimSettings.NodeFruitGrowthDelay > FruitTreeSimSettings.FruitGrowthTime)
    //         GD.PushWarning("Animation delay is bigger than growth time, which is not handled well.");
    //     base.VisualProcess(delta);
    // }

    /// <summary>
    /// Meant for manual animation of the forest growth from a saved forest.
    /// </summary>
    public void GrowAllTrees(float duration)
    {
        for (var i = 0; i < _entityManager.DataEntities.Count; i++)
        {
            (_entityManager.NodeEntities[i] as NodeTree)?.TweenToCorrectScale(_entityManager.DataEntities[i].Age, duration);
        }
    }

    /// <summary>
    /// Meant for growing all trees in the context of an AnimationSequence, making it scrubbable.
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Animation AnimateGrowingAllTrees(float duration)
    {
        var numTrees = _entityManager.DataEntities.Count;
        var animations = new Animation[numTrees];
        for (var i = 0; i < _entityManager.DataEntities.Count; i++)
        {
            animations[i] = (_entityManager.NodeEntities[i] as NodeTree)?.ScaleTo(
                NodeTree.ScaleFromAge(_entityManager.DataEntities[i].Age), 
                duration);
        }
        return AnimationUtilities.Parallel(animations);
    }

    public void CullTinyTrees()
    {
        foreach (var entity in _entityManager.NodeEntities)
        {
            var tree = entity as NodeTree;
            if (tree == null) continue;
            
            if (tree.Scale.X < 0.2f)
            {
                tree.QueueFree();
            }
        }
    }
    
    public override void _ExitTree()
    {
        if (_entityManager != null)
        {
            _entityManager.UnsubscribeFromEvents();
        }
        
        base._ExitTree();
    }
}
