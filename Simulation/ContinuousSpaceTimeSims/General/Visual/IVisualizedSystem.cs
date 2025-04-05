namespace PrimerTools.Simulation;

public interface IVisualizedSystem
{
    IVisualEventManager CreateVisualEventManager(VisualEntityRegistry visualEntityRegistry);
}