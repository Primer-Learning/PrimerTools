using Godot;
using PrimerTools.Simulation;
using PrimerTools.Simulation.Components;
using PrimerTools.Simulation.Visual;

namespace PrimerTools.Simulation.Visual;

public class FruitVisualEventManager : IVisualEventManager
{
    private readonly VisualEntityRegistry _visualRegistry;

    public FruitVisualEventManager(VisualEntityRegistry visualRegistry)
    {
        _visualRegistry = visualRegistry;
        _visualRegistry.RegisterEntityType<FruitVisualEntity>();
        _visualRegistry.SubscribeToComponentEvents<FruitComponent, FruitVisualEntity>();

        // Subscribe to fruit events
        // FruitSystem.FruitRipenedEvent += OnFruitRipened;
        // FruitSystem.FruitDetachedEvent += OnFruitDetached;
        FruitSystem.FruitDecayedEvent += OnFruitDecayed;
    }
    
    // private void OnFruitRipened(EntityId entityId)
    // {
    //     if (_visualRegistry.TryGetVisualEntity<FruitVisualEntity>(entityId, out var fruitVisual))
    //     {
    //         fruitVisual.HandleRipened();
    //     }
    // }
    //
    // private void OnFruitDetached(EntityId entityId)
    // {
    //     if (_visualRegistry.TryGetVisualEntity<FruitVisualEntity>(entityId, out var fruitVisual))
    //     {
    //         fruitVisual.HandleDetached();
    //     }
    // }
    //
    private void OnFruitDecayed(EntityId entityId)
    {
        _visualRegistry.GetVisualEntity<FruitVisualEntity>(entityId).HandleDecayed();
    }

    public void Cleanup()
    {
        // FruitSystem.FruitRipenedEvent -= OnFruitRipened;
        // FruitSystem.FruitDetachedEvent -= OnFruitDetached;
        FruitSystem.FruitDecayedEvent -= OnFruitDecayed;
    }
}
