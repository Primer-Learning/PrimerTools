using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public partial class VisualEntityRegistry : Node3D
{
    private readonly Dictionary<EntityId, IVisualEntity> _visualEntities = new();
    private readonly Dictionary<Type, Func<IVisualEntity>> _entityFactories = new();
    
    // For entities that don't need factories
    public void RegisterEntityType<T>() where T : VisualEntity, new()
    {
        _entityFactories[typeof(T)] = () => new T();
    }

    // For entities that need factories
    public void RegisterEntityType<T>(IVisualEntityFactory<T> factory) where T : IVisualEntity
    {
        _entityFactories[typeof(T)] = () => factory.CreateInstance();
    }
    private readonly EntityRegistry _entityRegistry;

    public VisualEntityRegistry(EntityRegistry entityRegistry)
    {
        _entityRegistry = entityRegistry;
    }
    
    public void SubscribeToComponentEvents<TComponent, TVisualEntity>()
        where TComponent : struct, IComponent
        where TVisualEntity : IVisualEntity
    {
        var storage = _entityRegistry.GetComponentStorageReader<TComponent>();
        storage.ComponentAdded += (component) => CreateVisualEntity<TVisualEntity>(component.EntityId);
        storage.ComponentRemoved += RemoveVisualEntity;
    }
    
    public void CreateVisualEntity<T>(EntityId entityId) where T : IVisualEntity
    {
        if (!_entityFactories.TryGetValue(typeof(T), out var factory))
        {
            throw new InvalidOperationException($"No factory registered for visual entity type {typeof(T)}");
        }

        var visualEntity = factory();
        AddChild(visualEntity.RootNode);
        _visualEntities.Add(entityId, visualEntity);
        visualEntity.Initialize(_entityRegistry, entityId);
    }

    public void RemoveVisualEntity(EntityId entityId)
    {
        if (_visualEntities.TryGetValue(entityId, out var entity))
        {
            // We don't queuefree here. Instead, we let the visual entity handle its own disappearance.
            // This could probably be handled in a more robust way, but if we ever fail to make a visual entity
            // clean itself up, it will be visually obvious.
            // entity.RootNode.QueueFree();
            _visualEntities.Remove(entityId);
        }
    }

    public void Update()
    {
        foreach (var visualEntity in _visualEntities.Values)
        {
            visualEntity.Update(_entityRegistry);
        }
    }
    

    public T GetVisualEntity<T>(EntityId entityId) where T : VisualEntity
    {
        if (!_visualEntities.TryGetValue(entityId, out var value))
        {
            GD.PrintErr("Tried to find visual entity that doesn't exist");
        }
        return (T)value;
    }
}
