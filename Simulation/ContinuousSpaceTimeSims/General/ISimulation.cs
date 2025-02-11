namespace PrimerTools.Simulation;

public interface ISimulation
{
    // This exists in addition to the abstract Simulation base class because that one it generic and can't be used
    // as a collection type.
    void Reset();
    void Step();
    void ClearDeadEntities();
}
