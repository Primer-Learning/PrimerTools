using Godot;
using PrimerTools;

namespace PrimerTools.Simulation.Aging;

[Tool]
public partial class SimulationWorld : Node3D
{
    [Export] public Vector2 WorldDimensions = Vector2.One * 50;
    [Export] public int PhysicsStepsPerRealSecond = 60;
    [Export] private int _seed = -1;

    public const int PhysicsStepsPerSimSecond = 60;

    private Rng _rng;
    public Rng Rng => _rng ??= new Rng(_seed == -1 ? System.Environment.TickCount : _seed);

    public World3D World3D { get; private set; }

    public override void _Ready()
    {
        World3D = GetWorld3D();
        PhysicsServer3D.SetActive(true);
        Engine.PhysicsTicksPerSecond = PhysicsStepsPerRealSecond;
    }

    public bool IsWithinWorldBounds(Vector3 position)
    {
        return position.X >= 0 && position.X <= WorldDimensions.X &&
               position.Z >= 0 && position.Z <= WorldDimensions.Y;
    }
}
