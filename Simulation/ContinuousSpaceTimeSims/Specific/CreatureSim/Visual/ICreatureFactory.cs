using PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public interface ICreatureFactory : IVisualEntityFactory<CreatureVisualEntity>
{
    ICreatureModelHandler CreateModelHandler();
    
    CreatureVisualEntity IVisualEntityFactory<CreatureVisualEntity>.CreateInstance()
    {
        return new CreatureVisualEntity(CreateModelHandler());
    }
}
