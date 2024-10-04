using System.Collections.Generic;
using System.Linq;
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
            creature.Actions = ActionFlags.None;
            
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

            // When this block is commented out, the movement works properly
            // 
            LabeledCollision closestFood;
            if (creature.Energy < creature.HungerThreshold)
            {
                closestFood = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Tree);
                if (closestFood.Type == CollisionType.Tree)
                {
                    if ((closestFood.Position - creature.Position).LengthSquared() <
                        CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance
                        && creature.EatingTimeLeft <= 0)
                    {
                        creature = CreatureSimSettings.EatFood(creature, closestFood.Index, index);
                        registry.Entities[index] = creature;
                        return;
                    }
            
                    creature.CurrentDestination = closestFood.Position;
                    creature.Actions |= ActionFlags.Move;
                    registry.Entities[index] = creature;
                    return;
                }
            }
            
            // LabeledCollision closestMate;
            // if (creature.Energy < creature.HungerThreshold) closestFood = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Tree);
            

            creature.Actions |= ActionFlags.Move;
            if ((creature.CurrentDestination - creature.Position).LengthSquared() <
                CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance)
            {
                creature.CurrentDestination = CreatureSimSettings.GetRandomDestination(creature.Position);
            }
            registry.Entities[index] = creature;
        }
    }
}
