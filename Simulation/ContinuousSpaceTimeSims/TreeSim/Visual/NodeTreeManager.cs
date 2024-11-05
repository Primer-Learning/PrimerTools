using Godot;
using PrimerTools;
using PrimerTools.Simulation;

public partial class NodeTreeManager : NodeEntityManager<DataTree, NodeTree>
{
    // Currently useless, but good for tree sim events in the future
    // Just here for symmetry with NodeCreatureManager
    
    public NodeTreeManager(DataEntityRegistry<DataTree> dataEntityRegistry) 
        : base(dataEntityRegistry) {}

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
}
