namespace PrimerTools.Simulation;

public interface ISimulation
{
    public void Initialize();
    public void Step();
    public void Reset();

}
