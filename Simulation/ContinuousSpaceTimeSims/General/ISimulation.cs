namespace PrimerTools.Simulation;

public interface ISimulation
{
    void Reset();
    void Step();
    void ClearDeadEntities();
}
