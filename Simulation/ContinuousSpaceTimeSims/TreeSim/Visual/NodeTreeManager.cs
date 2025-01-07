using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public partial class NodeTreeManager : NodeEntityManager<DataTree, NodeTree>
{
    public NodeTreeManager(DataEntityRegistry<DataTree> dataEntityRegistry) 
        : base(dataEntityRegistry) {}
    public NodeTreeManager(){}

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
        for (var i = 0; i < DataEntityRegistry.Entities.Count; i++)
        {
            NodeEntities[i].TweenToCorrectScale(DataEntityRegistry.Entities[i].Age, duration);
        }
    }

    /// <summary>
    /// Meant for growing all trees in the context of an AnimationSequence, making it scrubbable.
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Animation AnimateGrowingAllTrees(float duration)
    {
        var numTrees = DataEntityRegistry.Entities.Count;
        var animations = new Animation[numTrees];
        for (var i = 0; i < DataEntityRegistry.Entities.Count; i++)
        {
            animations[i] = NodeEntities[i].ScaleTo(NodeTree.ScaleFromAge(DataEntityRegistry.Entities[i].Age), duration);
        }
        return AnimationUtilities.Parallel(animations);
    }

    public void CullTinyTrees()
    {
        foreach (var tree in NodeEntities)
        {
            if (tree.Scale.X < 0.2f)
            {
                tree.QueueFree();
            }
        }
    }
}
