using Godot;

namespace PrimerTools.Simulation;

public interface IVisualEntityWithModel<TModelHandler> : IVisualEntity 
    where TModelHandler : IVisualModelHandler
{
    TModelHandler ModelHandler { get; }
}
