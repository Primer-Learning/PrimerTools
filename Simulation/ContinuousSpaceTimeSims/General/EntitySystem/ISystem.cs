using System;

namespace PrimerTools.Simulation;

public interface ISystem
{
    void Initialize(EntityRegistry registry, SimulationWorld simulationWorld);
    void Update(float deltaTime);
    event Action Stepped;
}
