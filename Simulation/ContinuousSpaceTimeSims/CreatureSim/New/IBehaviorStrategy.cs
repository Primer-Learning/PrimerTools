using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Schema;
using Godot;

namespace PrimerTools.Simulation.New
{
    public interface IBehaviorStrategy
    {
        void DetermineAction(int index, List<LabeledCollision> labeledCollisions, DataEntityRegistry<DataCreature> registry, IReproductionStrategy reproductionStrategy);
    }

    public class SimpleBehaviorStrategy : IBehaviorStrategy
    {
        public void DetermineAction(int index, List<LabeledCollision> labeledCollisions, DataEntityRegistry<DataCreature> registry, IReproductionStrategy reproductionStrategy)
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
            
            if (creature.OpenToMating)
            {
                var mateIndex = reproductionStrategy.FindMateIndex(index, registry);
                if (mateIndex != -1)
                {
                    var mate = registry.Entities[mateIndex];

                    if ((mate.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.CreatureMateDistance))
                    {
                        // TODO: Move this to the batch processing method
                        // Or perhaps not? Since the context of who the mate is will be lost.

                        mate.MatingTimeLeft += CreatureSimSettings.ReproductionDuration;
                        creature.MatingTimeLeft += CreatureSimSettings.ReproductionDuration;
                        mate.Energy -= CreatureSimSettings.ReproductionEnergyCost / 2;
                        creature.Energy -= CreatureSimSettings.ReproductionEnergyCost / 2;
                        registry.RegisterEntity(reproductionStrategy.Reproduce(creature, mate));
                        registry.Entities[index] = creature;
                        registry.Entities[mateIndex] = mate;
                        return;
                    }

                    creature.CurrentDestination = mate.Position;
                    creature.Actions |= ActionFlags.Move;
                    registry.Entities[index] = creature;
                    return;
                }
            }

            if (creature.Energy < creature.HungerThreshold)
            {
                var closestFood = labeledCollisions.FirstOrDefault(c => c.Type == CollisionType.Tree);
                if (closestFood.Type == CollisionType.Tree)
                {
                    if ((closestFood.Position - creature.Position).IsLengthLessThan(CreatureSimSettings.CreatureEatDistance)
                        && creature.EatingTimeLeft <= 0)
                    {
                        creature.FoodTargetIndex = closestFood.Index;
                        creature.Actions |= ActionFlags.Eat;
                        registry.Entities[index] = creature;
                        return;
                    }
            
                    creature.CurrentDestination = closestFood.Position;
                    creature.Actions |= ActionFlags.Move;
                    registry.Entities[index] = creature;
                    return;
                }
            }

            if (creature.MatingTimeLeft > 0)
            {
                creature.MatingTimeLeft = Mathf.Max(0, creature.MatingTimeLeft - SimulationWorld.TimeStep);
                registry.Entities[index] = creature;
                return;
            }

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
