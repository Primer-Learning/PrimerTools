using Godot;
using PrimerTools.Simulation;

public partial class NodeTreeManager : NodeEntityManager<DataTree, NodeTree>
{
    // Currently useless, but good for tree sim events in the future
    // Just here for symmetry with NodeCreatureManager
    
    public NodeTreeManager(DataEntityRegistry<DataTree> dataEntityRegistry) 
        : base(dataEntityRegistry) {}
}
