using Godot;
using PrimerTools.Simulation;

public partial class NodeTreeManager : NodeEntityManager<DataTree, NodeTree>
{
    public NodeTreeManager(DataEntityRegistry<DataTree> dataEntityRegistry) 
        : base(dataEntityRegistry)
    {
        // Subscribe to any tree-specific events here if needed
    }

    // Add any tree-specific methods or event handlers here

    public override void _ExitTree()
    {
        base._ExitTree();
        // Unsubscribe from any tree-specific events here if needed
    }
}
