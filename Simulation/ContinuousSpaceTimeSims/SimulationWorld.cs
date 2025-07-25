using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Simulation;

public enum VisualizationMode
{
    None,
    NodeCreatures
}

[Tool]
public partial class SimulationWorld : Node3D
{
    private PackedScene _defaultGroundScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Simulation/Models/Ground/round_ground.tscn");
    public const string GroundNameString = "Ground";

    private Node3D _ground;
    public Node3D Ground
    {
        get => _ground ?? GetNodeOrNull<Node3D>("../" + GroundNameString);
        set
        {
            // Free existing ground if it exists
            var existingGround = GetNodeOrNull<Node3D>("../" + GroundNameString);
            if (existingGround != null && IsInstanceValid(existingGround))
            {
                existingGround.Free();
            }

            _ground = value;
            if (_ground != null)
            {
                GetParent().AddChild(_ground);
                _ground.Name = GroundNameString;
            }
        }
    }
    
    
    #region Editor controls

    private bool _running;

    public bool Running
    {
        get => _running;
        set
        {
            _running = value;
            // GD.Print($"SimulationWorld running : {value}");
        }
    }

    public void Start()
    {
        GD.Print($"Starting sim at timescale: {_timeScale}");
        Running = true;
    }

    public void Stop()
    {
        Running = false;
    }
    
    [Export] public VisualizationMode VisualizationMode = VisualizationMode.NodeCreatures;
    
    [Export]
    public Vector3 WorldDimensions
    {
        get
        {
            return _worldMax - _worldMin;
        }
        set
        {
            GD.PushWarning("WorldDimensions is being deprecated. Use WorldMin and WorldMax instead.");
            _worldMin = -value / 2;
            _worldMax = value / 2;
        }
    }

    private Vector3 _worldMin = -Vector3.One * 50;
    public Vector3 WorldMin
    {
        get => _worldMin;
        set => _worldMin = value;
    }
    
    private Vector3 _worldMax = Vector3.One * 50;
    public Vector3 WorldMax
    {
        get => _worldMax;
        set => _worldMax = value;
    }

    // TODO: Track down some inconsistency with different time scales
    // It was happening in a scene and could be the scene's fault, but I'm not sure.
    private static float _timeScale = 1;
    public static event Action<float> TimeScaleChanged;
    // TODO: Make this not static. Having both this and TimeScaleControl (for exporting and keyframing) is just weird
    public static float TimeScale
    {
        get => _timeScale;
        set
        {
            _timeScale = value;
            Engine.PhysicsTicksPerSecond = (int)(value * PhysicsStepsPerSimSecond);
            Engine.MaxPhysicsStepsPerFrame = (int)(value * PhysicsStepsPerSimSecond);
            TimeScaleChanged?.Invoke(value);
        }
    }

    [Export]
    public float TimeScaleControl
    {
        get => TimeScale;
        set => TimeScale = value;
    }
    
    private static int _seed = -1;
    [Export]
    public int Seed
    {
        get => _seed;
        set
        {
            _seed = value;
            _rng = new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
        }
    }
    #endregion

    public const int PhysicsStepsPerSimSecond = 60;
    public const float TimeStep = 1f / PhysicsStepsPerSimSecond;
    public int PhysicsStepsTaken { get; private set; }
    public float TimeElapsed => PhysicsStepsTaken * TimeStep;

    private Rng _rng;
    public Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);
    public World3D World3D => GetWorld3D();
    
    private readonly List<ISystem> _systems = new();
    public IReadOnlyList<ISystem> Systems => _systems.AsReadOnly();
    private EntityRegistry _registry;
    public EntityRegistry Registry => _registry;
    private VisualEntityRegistry _visualEntityRegistry;
    private List<IVisualEventManager> _visualEventManagers = new();
    
    public T AddSystem<T>() where T : ISystem, new()
    {
        if (_systems.Any(x => x.GetType() == typeof(T)))
        {
            GD.PrintErr($"{typeof(T)} already added to SimulationWorld");
            return new T();
        }

        var system = new T();
        system.Initialize(_registry, this);
        _systems.Add(system);

        if (VisualizationMode == VisualizationMode.NodeCreatures && system is IVisualizedSystem visualizedSystem)
        {
            _visualEventManagers.Add(visualizedSystem.CreateVisualEventManager(_visualEntityRegistry));
        }

        return system;
    }
    
    public void Reset()
    {
        Running = false;

        foreach (var system in _systems)
        {
            if (system is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _systems.Clear();
        
        foreach (var visualEventManager in _visualEventManagers)
        {
            visualEventManager.Cleanup();
        }
        _visualEventManagers.Clear();
    }
    
    private static SimulationWorld _instance;
    public static SimulationWorld Instance
    {
        get
        {
            if (_instance == null)
            {
                GD.PrintErr("SimulationWorld instance is null. Make sure a SimulationWorld exists in the scene before accessing it.");
            }
            return _instance;
        }
    }
    
    public SimulationWorld()
    {
        if (_instance != null)
        {
            GD.PrintErr("Attempting to create a second SimulationWorld. Only one instance should exist.");
            QueueFree();
            return;
        }

        _instance = this;
        
        PhysicsServer3D.SetActive(true);
        Engine.PhysicsTicksPerSecond = (int) (_timeScale * PhysicsStepsPerSimSecond);
        _realTimeOfLastStatusPrint = System.Environment.TickCount;
        
        _registry = new EntityRegistry();
        _visualEntityRegistry = new VisualEntityRegistry(_registry);
        AddChild(_visualEntityRegistry);
        _visualEntityRegistry.Name = "Entity Node Parent";
    }

    public override void _Ready()
    {
        // Only create ground if it doesn't already exist
        if (Ground == null)
        {
            Ground = _defaultGroundScene.Instantiate<Node3D>();
            Ground.Scale = new Vector3(WorldDimensions.X, WorldDimensions.Y, WorldDimensions.Z);
            Ground.Position = new Vector3(
                (_worldMax.X + _worldMin.X) / 2,
                _worldMin.Y,
                (_worldMax.Z + _worldMin.Z) / 2
            );
        }
    }

    private int _realTimeOfLastStatusPrint;
    private int _stepsSinceLastStatusPrint;
    [Export] private float _statusPrintSimTimeInterval;

    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        // GD.Print("Running");
        foreach (var system in _systems)
        {
            system.Update((float)delta);
        }
        // Update physics after other systems
        PhysicsStepsTaken++;
        
        if (_statusPrintSimTimeInterval == 0) return;
        _stepsSinceLastStatusPrint++;

        if ((float)_stepsSinceLastStatusPrint / PhysicsStepsPerSimSecond >= _statusPrintSimTimeInterval)
        {
            var currentTime = System.Environment.TickCount;
            var differenceInSeconds = (currentTime - _realTimeOfLastStatusPrint) / 1000f; 
            GD.Print($"{_statusPrintSimTimeInterval} sim seconds in {differenceInSeconds} real seconds");
            _stepsSinceLastStatusPrint = 0;
            _realTimeOfLastStatusPrint = currentTime;
        }
    }
    public override void _Process(double delta)
    {
        if (!Running) return;
        _visualEntityRegistry.Update();
    }
    public bool IsWithinWorldBounds(Vector3 position, float margin = 0)
    {
        return position.X >= _worldMin.X - margin && position.X <= _worldMax.X + margin &&
               position.Y >= _worldMin.Y - margin && position.Y <= _worldMax.Y + margin &&
               position.Z >= _worldMin.Z - margin && position.Z <= _worldMax.Z + margin;
    }
    public Vector3 GetRandomDestination(Vector3 position, float maxDistance, float minDistance = 0)
    {
        Vector3 newDestination;
        var attempts = 0;
        const int maxAttempts = 100;
        
        if (minDistance > maxDistance) PrimerGD.PushWarningWithStackTrace("minDistance is greater than maxDistance, ya dummy" );

        do
        {
            // Generate random spherical coordinates
            var theta = _rng.RangeFloat(1) * 2 * Mathf.Pi; // Azimuthal angle
            var phi = _rng.RangeFloat(1) * Mathf.Pi;       // Polar angle
            var r = _rng.RangeFloat(minDistance, maxDistance);
            
            // Convert to Cartesian coordinates
            var displacement = new Vector3(
                r * Mathf.Sin(phi) * Mathf.Cos(theta),
                r * Mathf.Cos(phi),                    // Y component for height
                r * Mathf.Sin(phi) * Mathf.Sin(theta)
            );
            
            newDestination = position + displacement;
            attempts++;

            if (attempts >= maxAttempts)
            {
                GD.PrintErr($"Failed to find a valid destination after {maxAttempts} attempts. Using current position.");
                newDestination = position;
                break;
            }
        } while (!IsWithinWorldBounds(newDestination));

        return newDestination;
    }

    public float FractionOfDisplacementThatWouldGetYouToTheWorldBoundary(Vector3 currentPosition, Vector3 intendedDestination)
    {
        var tMin = float.MaxValue;
        var displacement = intendedDestination - currentPosition;
        
        // Check intersection with each boundary plane
        // tMin is the fraction of the displacement that would bring the intended position to the boundary
        
        // X boundaries
        if (displacement.X != 0)
        {
            if (currentPosition.X + displacement.X < _worldMin.X)
                tMin = Mathf.Min(tMin, (_worldMin.X - currentPosition.X) / displacement.X);
            if (currentPosition.X + displacement.X > _worldMax.X)
                tMin = Mathf.Min(tMin, (_worldMax.X - currentPosition.X) / displacement.X);
        }
        
        // Y boundaries
        if (displacement.Y != 0)
        {
            if (currentPosition.Y + displacement.Y < _worldMin.Y)
                tMin = Mathf.Min(tMin, (_worldMin.Y - currentPosition.Y) / displacement.Y);
            if (currentPosition.Y + displacement.Y > _worldMax.Y)
                tMin = Mathf.Min(tMin, (_worldMax.Y - currentPosition.Y) / displacement.Y);
        }
        
        // Z boundaries
        if (displacement.Z != 0)
        {
            if (currentPosition.Z + displacement.Z < _worldMin.Z)
                tMin = Mathf.Min(tMin, (_worldMin.Z - currentPosition.Z) / displacement.Z);
            if (currentPosition.Z + displacement.Z > _worldMax.Z)
                tMin = Mathf.Min(tMin, (_worldMax.Z - currentPosition.Z) / displacement.Z);
        }

        return tMin;
    }
    public Vector3 ClampDestinationToWorldBounds(Vector3 currentPosition, Vector3 intendedDestination)
    {
        // TODO: Make this more elegant and general by working with arbitrary boundaries rather that axis-aligned ones.
        
        // If the destination is already within bounds, just return it
        if (IsWithinWorldBounds(intendedDestination))
        {
            return intendedDestination;
        }
        
        var tMin = FractionOfDisplacementThatWouldGetYouToTheWorldBoundary(currentPosition, intendedDestination);
        
        // Apply a small offset to ensure we're just inside the boundary
        const float epsilon = 0.001f;
        tMin = Math.Max(0, tMin - epsilon);
        
        // Calculate the new destination
        return currentPosition + (intendedDestination - currentPosition) * tMin;
    }
}


