namespace PrimerTools.Simulation.Components;

public struct TreeComponent : IComponent
{
    public EntityId EntityId { get; set; }
    
    // public Vector3 Position;
    // public AreaComponent Body;
    
    public float Angle;
    public float Age;
    public bool IsMature => Age >= FruitTreeSimSettings.TreeMaturationTime;
    public float TimeSinceLastSpawn;
    public bool Alive { get; set; }
    public bool HasFruit;
    public float FruitGrowthProgress;
    
    public TreeComponent(float angle) : this()
    {
        Alive = true;
        // var shape = new SphereShape3D { Radius = 1.0f };
        // Position = position;
        // Body.Initialize(space, Transform3D.Identity.Translated(Position), Transform3D.Identity, shape);
        Angle = angle;
    }
    
    // public Rid GetBodyRid()
    // {
    //     return Body.Area;
    // }
    public void CleanUp()
    {
        // Body.CleanUp();
    }
}