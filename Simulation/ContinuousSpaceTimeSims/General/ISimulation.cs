namespace PrimerTools.Simulation;

public interface ISimulation
{
    bool Running { get; set; }
    void Initialize();
    void Reset();
    void Step();
    void VisualProcess(double delta);
    void ClearDeadEntities();
}
