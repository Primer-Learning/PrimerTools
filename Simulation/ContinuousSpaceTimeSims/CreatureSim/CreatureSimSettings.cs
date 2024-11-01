namespace PrimerTools.Simulation;

public class CreatureSimSettings
{
    private static CreatureSimSettings _instance;
    public static CreatureSimSettings Instance => _instance ??= new();

    private CreatureSimSettings() { }

    // Movement
    public float CreatureStepMaxLength { get; set; } = 10f;
    public float MaxAccelerationFactor { get; set; } = 0.1f;
    public float CreatureEatDistance { get; set; } = 2f;
    public float CreatureMateDistance { get; set; } = 2f;

    // State-based pause durations
    public float EatDuration { get; set; } = 1.5f;
    public float MaturationTime { get; set; } = 2f;

    // Energy
    public float BaseEnergySpend { get; set; } = 0.1f;
    public float GlobalEnergySpendAdjustmentFactor { get; set; } = 0.2f;
    public float MinEnergyGainFromFood { get; set; } = 0.5f;
    public float MaxEnergyGainFromFood { get; set; } = 1.5f;
    public float ReproductionEnergyThreshold { get; set; } = 2f;
    public float ReproductionEnergyCost { get; set; } = 1f;
    public float DefaultHungerThreshold { get; set; } = 4f;
    public float ReproductionDuration { get; set; } = 1f;

    // Initial population
    public float ReferenceCreatureSpeed { get; set; } = 5f;
    public float ReferenceAwarenessRadius { get; set; } = 5f;
    public float ReferenceMaxAge { get; set; } = 20f;

    // Mutation
    public float MutationProbability { get; set; } = 0.1f;
    public float MutationIncrement { get; set; } = 1f;
    public float DeleteriousMutationRate { get; set; } = 0f;

    // Delegates
    public FindMateDelegate FindMate { get; set; } = MateSelectionStrategies.FindFirstAvailableMate;
    public ReproduceDelegate Reproduce { get; set; } = ReproductionStrategies.SexualReproduce;
    public InitialPopulationGeneratorDelegate InitializePopulation { get; set; } = 
        InitialPopulationGeneration.DefaultSpeedAndAwarenessDiploid;

    public void ResetToDefaults()
    {
        CreatureStepMaxLength = 10f;
        MaxAccelerationFactor = 0.1f;
        CreatureEatDistance = 2f;
        CreatureMateDistance = 2f;
        EatDuration = 1.5f;
        MaturationTime = 2f;
        BaseEnergySpend = 0.1f;
        GlobalEnergySpendAdjustmentFactor = 0.2f;
        MinEnergyGainFromFood = 0.5f;
        MaxEnergyGainFromFood = 1.5f;
        ReproductionEnergyThreshold = 2f;
        ReproductionEnergyCost = 1f;
        DefaultHungerThreshold = 4f;
        ReproductionDuration = 1f;
        ReferenceCreatureSpeed = 5f;
        ReferenceAwarenessRadius = 5f;
        ReferenceMaxAge = 20f;
        MutationProbability = 0.1f;
        MutationIncrement = 1f;
        DeleteriousMutationRate = 0f;
        FindMate = MateSelectionStrategies.FindFirstAvailableMate;
        Reproduce = ReproductionStrategies.SexualReproduce;
        InitializePopulation = InitialPopulationGeneration.DefaultSpeedAndAwarenessDiploid;
    }
}
