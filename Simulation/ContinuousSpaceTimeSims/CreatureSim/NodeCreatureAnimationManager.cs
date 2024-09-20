namespace PrimerTools.Simulation;

public partial class NodeCreatureAnimationManager : NodeAnimationManager<DataCreature, NodeCreature>
{
    public CreatureSim CreatureSim;

    public NodeCreatureAnimationManager(SimulationWorld simulationWorld) : base(simulationWorld) {}
    public NodeCreatureAnimationManager() {}

    public override void VisualProcess(double delta)
    {
        if (CreatureSim == null) return;

        for (var i = 0; i < CreatureSim.Registry.Entities.Count; i++)
        {
            Entities[i].UpdateTransform(CreatureSim.Registry.Entities[i]);
        }
    }
}
