using Godot;

namespace PrimerTools.Simulation;
public class FruitTreeSimSettings
{
    #region Distribution Save/Load
    public static string TreeDistributionPath { get; set; }
    public bool LoadTreeDistribution => !string.IsNullOrEmpty(TreeDistributionPath);
    #endregion
    
    #region Tree settings
    public static float MaxTreeSpawnRadius = 8f;
    public static float MinTreeSpawnRadius = 2f;
    public static float TreeCompetitionRadius = 6f;
    public static float MinimumTreeDistance = 2f;

    public const float TreeMaturationTime = 10f;
    public const float TreeSpawnInterval = 5f;

    public const float DeathCheckInterval = 0.2f;
    public const float SaplingDeathProbabilityBase = 0.0001f;
    public const float SaplingDeathProbabilityPerNeighbor = 0.01f;
    public const float MatureTreeDeathProbabilityBase = 0.0001f;
    public const float MatureTreeDeathProbabilityPerNeighbor = 0.0002f;
    #endregion
    
    #region Fruit Settings
    // Growth and lifecycle
    public static float FruitRipeningTime = 5.0f; // Time to ripen after fully grown
    public static float FruitDecayTime = 10.0f; // Time until fruit decays after falling
    
    public static float FruitGrowthTime = 4f;
    public static float NodeFruitGrowthDelay = 2f; // This is only visual. Kinda doesn't belong here.
    
    // Physics properties
    public static float FruitMass = 0.5f; // Mass of fruit for physics
    public static float FruitRadius = 0.925f; // Radius of fruit collision sphere
    
    // Probabilities
    public static float RipeFruitFallProbabilityPerSecond = 0.05f; // Chance per second for ripe fruit to fall
    
    // Standard fruit positions on tree (relative to tree origin)
    public static readonly Vector3[] StandardFruitPositions = new Vector3[]
    {
        new Vector3(0.8f, 1.5f, 0.6f),
        new Vector3(-0.7f, 1.7f, 0.5f),
        new Vector3(0.5f, 1.9f, -0.6f),
        new Vector3(-0.6f, 1.6f, -0.7f)
    };
    
    // Maximum number of fruits per tree
    public static int MaxFruitsPerTree = 1;
    #endregion
}
