using System;
using System.Collections.Generic;
using Godot;
using PrimerTools.Simulation.Components;

namespace PrimerTools.Simulation;

public class EntityRegistry
{
    private uint _nextEntityId = 1; // Start with 1 so Ids with value zero are invalid.
    private readonly Dictionary<Type, IComponentStorage> _componentStorages = new();
    private readonly HashSet<EntityId> _activeEntities = new();

    private static EntityRegistry _instance;
    public static EntityRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                GD.PrintErr("EntityRegistry instance is null. Make sure a EntityRegistry exists before accessing it.");
            }
            return _instance;
        }
    }

    public EntityRegistry()
    {
        if (_instance != null)
        {
            GD.PrintErr("Attempting to create a second EntityRegistry. Only one instance should exist.");
            return;
        }

        _instance = this;
    }

    public EntityId CreateEntity()
    {
        var entityId = new EntityId(_nextEntityId++);
        _activeEntities.Add(entityId);
        return entityId;
    }

    public void DestroyEntity(EntityId entityId)
    {
        if (!_activeEntities.Contains(entityId))
        {
            // GD.Print($"Attempted to destroy inactive entity {entityId.Value}");
            return;
        }
        
        // GD.Print($"Destroying entity {entityId.Value}");
        foreach (var storage in _componentStorages.Values)
        {
            storage.Remove(entityId);
        }
        _activeEntities.Remove(entityId);
    }
    public void AddComponent<T>(EntityId entityId, T component) where T : struct, IComponent
    {
        if (!entityId.IsValid)
        {
            throw new ArgumentException("Attempted to add component with default EntityId.");
        }
        
        // Handy logs
        // if (typeof(T) == typeof(TreeComponent)) GD.Print($"Registering tree with ID: {entityId}");
        // if (typeof(T) == typeof(PhysicsComponent)) GD.Print($"Registering physics with ID: {entityId}");
        
        var storage = GetOrCreateStorage<T>();
        if (storage.TryGet(entityId, out _))
        {
            throw new InvalidOperationException(
                $"Attempted to add component of type {typeof(T)} for entity {entityId.Value}, " +
                "but this entity already has this component type. Use UpdateComponent instead.");
        }

        component.EntityId = entityId;
        // If the entity has a physics body, register it with the collision registry
        // if (component is IPhysicsComponent physicsEntity)
        // {
        //     CollisionRegistry.RegisterBody(physicsEntity.GetBodyRid(), typeof(T), entityId);
        // }
        storage.Add(component);
    }

    public void UpdateComponent<T>(T component) where T : struct, IComponent
    {
        if (!component.EntityId.IsValid) 
        {
            throw new InvalidOperationException(
                $"Attempted to update component of type {typeof(T)} with invalid Id.");
        }
        var storage = GetOrCreateStorage<T>();
        if (!storage.TryGet(component.EntityId, out _))
        {
            throw new InvalidOperationException(
                $"Attempted to update component of type {typeof(T)} for entity {component.EntityId.Value}, " +
                "but this entity doesn't have this component type. Use AddComponent first.");
        }

        storage.Add(component);
    }

    // TODO: Either get rid of this or make it private. External users should get an error if they try to get a component that doesn't exist.
    public bool TryGetComponent<T>(EntityId entityId, out T component) where T : struct, IComponent
    {
        if (!_componentStorages.TryGetValue(typeof(T), out var storage))
        {
            component = default;
            return false;
        }

        return ((ComponentStorage<T>)storage).TryGet(entityId, out component);
    }
    public T GetComponent<T>(EntityId entityId) where T : struct, IComponent
    {
        if (TryGetComponent<T>(entityId, out var component))
        {
            return component;
        }
        PrimerGD.PrintWithStackTrace($"Tried to get a component of type {typeof(T)} that doesn't exist.");
        return new T();
    }

    public IEnumerable<T> GetComponents<T>() where T : struct, IComponent
    {
        var storage = GetOrCreateStorage<T>();
        return storage.GetAll();
    }

    private ComponentStorage<T> GetOrCreateStorage<T>() where T : struct, IComponent
    {
        var type = typeof(T);
        if (!_componentStorages.TryGetValue(type, out var storage))
        {
            storage = new ComponentStorage<T>();
            _componentStorages[type] = storage;
        }
        return (ComponentStorage<T>)storage;
    }
    
    public IComponentStorageReader<T> GetComponentStorageReader<T>() where T : struct, IComponent
    {
        return GetOrCreateStorage<T>();
    }
}

public interface IComponentStorageReader<T> where T : struct, IComponent
{
    event Action<T> ComponentAdded;
    event Action<EntityId> ComponentRemoved;
}

public interface IComponentStorage
{
    void Remove(EntityId entityId);
}

public class ComponentStorage<T> : IComponentStorage, IComponentStorageReader<T> where T : struct, IComponent
{
    private readonly Dictionary<EntityId, T> _components = new();

    public event Action<T> ComponentAdded;
    public event Action<EntityId> ComponentRemoved;

    internal void Add(T component)
    {
        var isNew = !_components.ContainsKey(component.EntityId);
        _components[component.EntityId] = component;
        if (isNew)
        {
            ComponentAdded?.Invoke(component);
        }
    }

    public bool TryGet(EntityId entityId, out T component)
    {
        return _components.TryGetValue(entityId, out component);
    }

    public void Remove(EntityId entityId)
    {
        if (_components.TryGetValue(entityId, out var component))
        {
            _components.Remove(entityId);
            component.CleanUp();
            ComponentRemoved?.Invoke(entityId);
        }
    }
    
    internal IEnumerable<T> GetAll()
    {
        return _components.Values;
    }
}
