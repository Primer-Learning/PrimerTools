
namespace PrimerTools.Simulation;
public class FruitTreeSimSettings
{
    #region Distribution Save/Load
    public static string TreeDistributionPath { get; set; }
    public bool LoadTreeDistribution => !string.IsNullOrEmpty(TreeDistributionPath);
    #endregion
    
    #region Simulation Parameters
    public static float MaxTreeSpawnRadius = 8f;
    public static float MinTreeSpawnRadius = 2f;
    public static float TreeCompetitionRadius = 6f;
    public static float MinimumTreeDistance = 2f;

    public const float TreeMaturationTime = 20f;
    public const float TreeSpawnInterval = 3f;
    public static float FruitGrowthTime = 4f;
    public static float NodeFruitGrowthDelay = 2f; // This is only visual. Kinda doesn't belong here.

    public const float SaplingDeathProbabilityBase = 0.0001f;
    public const float SaplingDeathProbabilityPerNeighbor = 0.001f;
    public const float MatureTreeDeathProbabilityBase = 0.0001f;
    public const float MatureTreeDeathProbabilityPerNeighbor = 0.0002f;
    #endregion
}
