namespace PrimerTools.Simulation;
public static class FruitTreeSimSettings
{
    #region Simulation Parameters
    public const float MaxTreeSpawnRadius = 5f;
    public const float MinTreeSpawnRadius = 1f;
    public const float TreeCompetitionRadius = 3f;
    public const float MinimumTreeDistance = 0.5f;

    public static float TreeMaturationTime = 1f;
    public const float TreeSpawnInterval = 0.4f;
    public static float FruitGrowthTime = 4f;
    public const float NodeFruitGrowthDelay = 2f;

    public const float SaplingDeathProbabilityBase = 0.001f;
    public const float SaplingDeathProbabilityPerNeighbor = 0.01f;
    public const float MatureTreeDeathProbabilityBase = 0.0001f;
    public const float MatureTreeDeathProbabilityPerNeighbor = 0.0001f;
    #endregion
}