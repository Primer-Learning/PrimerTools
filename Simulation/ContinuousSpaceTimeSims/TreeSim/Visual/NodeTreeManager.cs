using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public partial class NodeTreeManager : NodeEntityManager<DataTree>
{
    public NodeTreeManager(DataEntityRegistry<DataTree> dataEntityRegistry)
        : base(dataEntityRegistry, () => new NodeTree())
    {
    }

    public NodeTreeManager() : base(null, null) {}
    
    public NodeTree GetNodeEntityByDataID(Rid rid) => GetNodeEntityByDataID<NodeTree>(rid);

    public override void VisualProcess(double delta)
    {
        if (FruitTreeSimSettings.NodeFruitGrowthDelay > FruitTreeSimSettings.FruitGrowthTime)
            GD.PushWarning("Animation delay is bigger than growth time, which is not handled well.");
        base.VisualProcess(delta);
    }

    /// <summary>
    /// Meant for manual animation of the forest growth from a saved forest.
    /// </summary>
    public void GrowAllTrees(float duration)
    {
        for (var i = 0; i < DataEntities.Count; i++)
        {
            (NodeEntities[i] as NodeTree)?.TweenToCorrectScale(DataEntities[i].Age, duration);
        }
    }

    /// <summary>
    /// Meant for growing all trees in the context of an AnimationSequence, making it scrubbable.
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Animation AnimateGrowingAllTrees(float duration)
    {
        var numTrees = DataEntities.Count;
        var animations = new Animation[numTrees];
        for (var i = 0; i < DataEntities.Count; i++)
        {
            animations[i] = (NodeEntities[i] as NodeTree)?.ScaleTo(
                NodeTree.ScaleFromAge(DataEntities[i].Age), 
                duration);
        }
        return AnimationUtilities.Parallel(animations);
    }

    public void CullTinyTrees()
    {
        foreach (var entity in NodeEntities)
        {
            var tree = entity as NodeTree;
            if (tree == null) continue;
            
            if (tree.Scale.X < 0.2f)
            {
                tree.QueueFree();
            }
        }
    }
}
