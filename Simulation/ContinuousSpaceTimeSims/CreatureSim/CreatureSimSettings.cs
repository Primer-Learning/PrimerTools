namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public class CreatureSimSettings
{
    private static CreatureSimSettings _instance;
    public static CreatureSimSettings Instance => _instance ??= new();

    // Default values
    private const float DefaultCreatureStepMaxLength = 10f;
    private const float DefaultMaxAccelerationFactor = 0.1f;
    private const float DefaultCreatureEatDistance = 2f;
    private const float DefaultCreatureMateDistance = 2f;
    private const float DefaultEatDuration = 1.5f;
    private const float DefaultMaturationTime = 2f;
    private const float DefaultBaseEnergySpend = 0.1f;
    private const float DefaultGlobalEnergySpendAdjustmentFactor = 0.2f;
    private const float DefaultMinEnergyGainFromFood = 0.5f;
    private const float DefaultMaxEnergyGainFromFood = 1.5f;
    private const float DefaultReproductionEnergyThreshold = 2f;
    private const float DefaultReproductionEnergyCost = 1f;
    private const float DefaultHungerThreshold = 4f;
    private const float DefaultReproductionDuration = 1f;
    private const float DefaultReferenceCreatureSpeed = 5f;
    private const float DefaultReferenceAwarenessRadius = 5f;
    private const float DefaultReferenceMaxAge = 20f;
    private const float DefaultMutationProbability = 0.1f;
    private const float DefaultMutationIncrement = 1f;
    private const float DefaultDeleteriousMutationRate = 0f;

    private CreatureSimSettings() 
    {
        ResetToDefaults();
    }

    // Movement
    public float CreatureStepMaxLength { get; set; }
    public float MaxAccelerationFactor { get; set; }
    public float CreatureEatDistance { get; set; }
    public float CreatureMateDistance { get; set; }

    // State-based pause durations
    public float EatDuration { get; set; }
    public float MaturationTime { get; set; }

    // Energy
    public float BaseEnergySpend { get; set; }
    public float GlobalEnergySpendAdjustmentFactor { get; set; }
    public float MinEnergyGainFromFood { get; set; }
    public float MaxEnergyGainFromFood { get; set; }
    public float ReproductionEnergyThreshold { get; set; }
    public float ReproductionEnergyCost { get; set; }
    public float HungerThreshold { get; set; }
    public float ReproductionDuration { get; set; }

    // Initial population
    public float ReferenceCreatureSpeed { get; set; }
    public float ReferenceAwarenessRadius { get; set; }
    public float ReferenceMaxAge { get; set; }

    // Mutation
    public float MutationProbability { get; set; }
    public float MutationIncrement { get; set; }
    public float DeleteriousMutationRate { get; set; }

    // Delegates
    public FindMateDelegate FindMate { get; set; }
    public ReproduceDelegate Reproduce { get; set; }
    public InitialPopulationGeneratorDelegate InitializePopulation { get; set; }

    public void ResetToDefaults()
    {
        CreatureStepMaxLength = DefaultCreatureStepMaxLength;
        MaxAccelerationFactor = DefaultMaxAccelerationFactor;
        CreatureEatDistance = DefaultCreatureEatDistance;
        CreatureMateDistance = DefaultCreatureMateDistance;
        EatDuration = DefaultEatDuration;
        MaturationTime = DefaultMaturationTime;
        BaseEnergySpend = DefaultBaseEnergySpend;
        GlobalEnergySpendAdjustmentFactor = DefaultGlobalEnergySpendAdjustmentFactor;
        MinEnergyGainFromFood = DefaultMinEnergyGainFromFood;
        MaxEnergyGainFromFood = DefaultMaxEnergyGainFromFood;
        ReproductionEnergyThreshold = DefaultReproductionEnergyThreshold;
        ReproductionEnergyCost = DefaultReproductionEnergyCost;
        HungerThreshold = DefaultHungerThreshold;
        ReproductionDuration = DefaultReproductionDuration;
        ReferenceCreatureSpeed = DefaultReferenceCreatureSpeed;
        ReferenceAwarenessRadius = DefaultReferenceAwarenessRadius;
        ReferenceMaxAge = DefaultReferenceMaxAge;
        MutationProbability = DefaultMutationProbability;
        MutationIncrement = DefaultMutationIncrement;
        DeleteriousMutationRate = DefaultDeleteriousMutationRate;
        FindMate = MateSelectionStrategies.FindFirstAvailableMate;
        Reproduce = ReproductionStrategies.SexualReproduce;
        InitializePopulation = InitialPopulationGeneration.DefaultSpeedAndAwarenessDiploid;
    }
}
