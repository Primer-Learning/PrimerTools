namespace PrimerTools.Simulation.Components;

public struct TreeComponent : IComponent
{
    public EntityId EntityId { get; set; }
    
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
        Angle = angle;
    }
    
    public void CleanUp() {}
}