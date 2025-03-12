namespace PrimerTools.Simulation;

public interface IComponent
{
    EntityId EntityId { get; set; }
    void CleanUp();
}
