using Godot;

namespace PrimerTools.Simulation.New
{
    public interface IAction
    {
        void Execute(int index, DataEntityRegistry<DataCreature> registry);
    }

    public class MoveAction : IAction
    {
        private Vector3? _destination;

        public MoveAction() {}
        public MoveAction(Vector3 destination)
        {
            _destination = destination;
        }
        
        public void Execute(int index, DataEntityRegistry<DataCreature> registry)
        {
            var creature = registry.Entities[index];

            if (_destination != null)
            {
                creature.CurrentDestination = _destination.Value;
            }
            var destination = creature.CurrentDestination;

            creature.Velocity = UpdateVelocity(creature.Position, destination, creature.Velocity, creature.MaxSpeed);
            creature.Position += creature.Velocity / SimulationWorld.PhysicsStepsPerSimSecond;
            
            var transformNextFrame = new Transform3D(Basis.Identity, creature.Position);
            PhysicsServer3D.AreaSetTransform(creature.Body, transformNextFrame);
            PhysicsServer3D.AreaSetTransform(creature.Awareness, transformNextFrame);
            
            // CreatureSimSettings.SpendMovementEnergy(ref creature);
            
            registry.Entities[index] = creature;
        }
        
        private static Vector3 UpdateVelocity(Vector3 position, Vector3 destination, Vector3 currentVelocity, float maxSpeed)
        {
            var desiredDisplacement = destination - position;
            var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
		
            // If we're basically there, choose a new destination
            if (desiredDisplacementLengthSquared < CreatureSimSettings.CreatureEatDistance * CreatureSimSettings.CreatureEatDistance)
            {
                GD.PushWarning("Creature is already at its destination during UpdateVelocity, which shouldn't happen.");
			
                // ChooseDestination(ref creature);
                // desiredDisplacement = creature.CurrentDestination - creature.Position;
                // desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
            }
		
            // Calculate desired velocity
            var desiredVelocity = desiredDisplacement * maxSpeed / Mathf.Sqrt(desiredDisplacementLengthSquared);
		
            // Calculate velocity change
            var velocityChange = desiredVelocity - currentVelocity;
            var velocityChangeLengthSquared = velocityChange.LengthSquared();

            // Calculate acceleration vector with a maximum magnitude
            var maxAccelerationMagnitudeSquared = maxSpeed * maxSpeed * CreatureSimSettings.MaxAccelerationFactor * CreatureSimSettings.MaxAccelerationFactor;
            Vector3 accelerationVector;
            if (velocityChangeLengthSquared > maxAccelerationMagnitudeSquared)
            {
                accelerationVector =  Mathf.Sqrt(maxAccelerationMagnitudeSquared / velocityChangeLengthSquared) * velocityChange;
            }
            else
            {
                accelerationVector = velocityChange;
            }

            var newVelocity = currentVelocity + accelerationVector;
            // Limit velocity to max speed
            var velocityLengthSquared = newVelocity.LengthSquared();
            var maxSpeedSquared = maxSpeed * maxSpeed;
            if (velocityLengthSquared > maxSpeedSquared)
            {
                newVelocity = maxSpeed / Mathf.Sqrt(velocityLengthSquared) * newVelocity;
            }

            return newVelocity;
        }
    }

    public class EatAction : IAction
    {
        public int TreeIndex { get; }

        public EatAction(int treeIndex)
        {
            TreeIndex = treeIndex;
        }

        public void Execute(int index, DataEntityRegistry<DataCreature> registry)
        {
            var creature = registry.Entities[index];
            // Implementation
            CreatureSimSettings.EatFood(ref creature, TreeIndex, index);
            registry.Entities[index] = creature;
        }
    }

    public class ReproduceAction : IAction
    {
        public void Execute(int index, DataEntityRegistry<DataCreature> registry)
        {
            var creature = registry.Entities[index];
            var newCreature = CreatureSimSettings.CreatureSim.ReproductionStrategy.Reproduce(ref creature, registry);
            if (newCreature.Alive)
            {
                registry.RegisterEntity(newCreature);
            }
            registry.Entities[index] = creature;
        }
    }
}
