namespace PrimerTools.Simulation;

public interface IVisualEntityFactory<out T> where T : IVisualEntity
{
    T CreateInstance();
}
