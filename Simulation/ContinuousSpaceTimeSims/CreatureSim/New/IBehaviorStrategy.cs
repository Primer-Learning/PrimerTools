using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation.New
{
    public interface IBehaviorStrategy
    {
        void DetermineAction(int index, List<LabeledCollision> labeledCollisions, DataEntityRegistry<DataCreature> registry);
    }

    public class SimpleBehaviorStrategy : IBehaviorStrategy
    {
        public void DetermineAction(int index, List<LabeledCollision> labeledCollisions, DataEntityRegistry<DataCreature> registry)
        {
            var creature = registry.Entities[index];
            if (!creature.Alive) return;
            if (creature.Age < CreatureSimSettings.MaturationTime) return;
            if (creature.EatingTimeLeft > 0)
            {
                // TODO: Get rid of EatingTimeLeft. There could be an absolute time that is compared instead.
                // Meaning we don't have to update this manually.
                creature.EatingTimeLeft -= SimulationWorld.TimeStep;
                registry.Entities[index] = creature;
                return;
            }

            if ((creature.CurrentDestination - creature.Position).LengthSquared() <
                CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance)
            {
                var newDestination = CreatureSimSettings.GetRandomDestination(creature.Position);
                creature.CurrentDestination = newDestination;
            }

            registry.Entities[index] = creature;
        }
    }
}
