using Godot;

namespace PrimerTools.Simulation;

public struct DataTree : IDataEntity, IPhysicsEntity
{
    public BodyComponent Body;
    public Vector3 Position;
    public float Angle;
    public float Age;
    public bool IsMature => Age >= FruitTreeSimSettings.TreeMaturationTime;
    public float TimeSinceLastSpawn;
    public int EntityId { get; set; }
    public bool Alive { get; set; }
    public bool HasFruit;
    public float FruitGrowthProgress;
    
    public void CleanUp()
    {
        Body.CleanUp();
    }

    public void Initialize(Rid space)
    {
        var shape = new SphereShape3D { Radius = 1.0f };
        Body.Initialize(space, Position, shape);
        TimeSinceLastSpawn = 0;
        Alive = true;
    }

    public Rid GetBodyRid()
    {
        return Body.Area;
    }
}
