using System;
using System.Linq;

namespace PrimerTools.Simulation;

public class AreaPhysicsSystem : ISystem
{
    private EntityRegistry _registry;
    private SimulationWorld _simulationWorld;
    
    public event Action Stepped;

    public void Initialize(EntityRegistry registry, SimulationWorld simulationWorld)
    {
        _registry = registry;
        _simulationWorld = simulationWorld;
    }

    public void Update(float deltaTime)
    {
        foreach (var immutablePhysics in _registry.GetComponents<AreaPhysicsComponent>().ToArray())
        {
            var physics = immutablePhysics;
            physics.Position += physics.Velocity * deltaTime;
            physics.Velocity *= physics.VelocityDampingFactor;
            
            if (physics.AngularVelocity.LengthSquared() > 0.001f)
            {
                var angle = physics.AngularVelocity.Length() * deltaTime;
                physics.Transform = physics.Transform.RotatedLocal(physics.AngularVelocity.Normalized(), angle);
            }

            physics.AngularVelocity *= physics.AngularVelocityDampingFactor;
            
            physics.UpdateCollisionAreas();
            _registry.UpdateComponent(physics);
        }
    }
}
