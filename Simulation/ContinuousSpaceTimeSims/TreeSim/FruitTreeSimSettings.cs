
namespace PrimerTools.Simulation;
public class FruitTreeSimSettings
{
    #region Distribution Save/Load
    public string TreeDistributionPath { get; set; }
    public bool LoadTreeDistribution => !string.IsNullOrEmpty(TreeDistributionPath);
    #endregion
    
    #region Simulation Parameters
    public const float MaxTreeSpawnRadius = 7f;
    public const float MinTreeSpawnRadius = 1f;
    public const float TreeCompetitionRadius = 5f;
    public const float MinimumTreeDistance = 1f;

    public const float TreeMaturationTime = 1f;
    public const float TreeSpawnInterval = 0.4f;
    public static float FruitGrowthTime = 6f;
    public const float NodeFruitGrowthDelay = 2f; // This is only visual. Kinda doesn't belong here.

    public const float SaplingDeathProbabilityBase = 0.001f;
    public const float SaplingDeathProbabilityPerNeighbor = 0.01f;
    public const float MatureTreeDeathProbabilityBase = 0.0001f;
    public const float MatureTreeDeathProbabilityPerNeighbor = 0.0001f;
    #endregion
}
