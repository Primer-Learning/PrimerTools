// using System.Collections.Generic;
// using Godot;
//
// namespace PrimerTools.Simulation.New
// {
//     public interface IBehaviorStrategy
//     {
//         IAction DetermineAction(int index, List<LabeledCollision> labeledCollisions, DataEntityRegistry<DataCreature> registry);
//         bool ShouldChooseNewDestination(int index, DataEntityRegistry<DataCreature> registry);
//     }
//
//     public class SimpleBehaviorStrategy : IBehaviorStrategy
//     {
//         public IAction DetermineAction(int index, List<LabeledCollision> labeledCollisions, DataEntityRegistry<DataCreature> registry)
//         {
//             var creature = registry.Entities[index];
//             
//             if (ShouldChooseNewDestination(index, registry))
//             {
//                 return new MoveAction(CreatureSimSettings.GetRandomDestination(creature.Position));
//             }
//             
//             return new MoveAction();
//         }
//
//         public bool ShouldChooseNewDestination(int index, DataEntityRegistry<DataCreature> registry)
//         {
//             var creature = registry.Entities[index];
//             return (creature.CurrentDestination - creature.Position).LengthSquared() <
//                 CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance;
//         }
//     }
// }
