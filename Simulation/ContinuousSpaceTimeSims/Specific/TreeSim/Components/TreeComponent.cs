namespace PrimerTools.Simulation.Components;

public struct TreeComponent : IComponent
{
    public EntityId EntityId { get; set; }
    
    public float Age;
    public bool IsMature => Age >= FruitTreeSimSettings.TreeMaturationTime;
    public float TimeSinceLastSpawn;
    public bool Alive { get; set; }
    
    // Fruit tracking
    public float TimeSinceLastFruitCheck;
    public EntityId[] AttachedFruits;
    
    // Legacy fruit properties (to be removed after full transition)
    public bool HasFruit;
    public float FruitGrowthProgress;

    public BodyHandler Body;
    
    public TreeComponent() // TODO: Add number of flowers as a parameter here
    {
        Alive = true;
        AttachedFruits = new EntityId[FruitTreeSimSettings.MaxFruitsPerTree];
        for (var i = 0; i < AttachedFruits.Length; i++)
        {
            AttachedFruits[i] = new EntityId();
        }
    }
    
    public void CleanUp() {}
}
