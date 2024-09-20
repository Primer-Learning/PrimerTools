namespace PrimerTools.Simulation;

public partial class NodeTreeAnimationManager : NodeAnimationManager<DataTree, NodeTree>
{
    public FruitTreeSim TreeSim;

    public NodeTreeAnimationManager(SimulationWorld simulationWorld) : base(simulationWorld) {}
    public NodeTreeAnimationManager() {}

    public override void VisualProcess(double delta)
    {
        if (TreeSim == null) return;

        for (var i = 0; i < TreeSim.Registry.Entities.Count; i++)
        {
            Entities[i].UpdateTransform(TreeSim.Registry.Entities[i]);
        }
    }
}
