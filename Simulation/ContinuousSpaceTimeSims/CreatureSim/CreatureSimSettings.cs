namespace PrimerTools.Simulation;

public class CreatureSimSettings
{
	// Three categories
	// 1. Always constant (const/static)
	// 2. Can vary between sims but not between creatures (instance vars)
	// 3. Can vary between creatures and is subject to mutation (doesn't belong here)
	
	#region Sim parameters
	// Movement
	public const float CreatureStepMaxLength = 10f;
	public const float MaxAccelerationFactor = 0.1f;
	public const float CreatureEatDistance = 2;
	public const float CreatureMateDistance = 2;
	
	// State-based pause durations
	public const float EatDuration = 1.5f;
	public const float MaturationTime = 2f;
	
	// Energy
	public const float BaseEnergySpend = 0.1f;
	public const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	public const float MinEnergyGainFromFood = 0.5f;
	public const float MaxEnergyGainFromFood = 1.5f;
	public const float ReproductionEnergyThreshold = 2f;
	public const float ReproductionEnergyCost = 1f;
	public const float DefaultHungerThreshold = 4;
	public const float ReproductionDuration = 1;
	
	// Initial population
	public const float ReferenceCreatureSpeed = 5f;
	public const float ReferenceAwarenessRadius = 5f;
	public const float ReferenceMaxAge = 20;
	
	// Mutation
	public const float MutationProbability = 0.1f;
	public const float MutationIncrement = 1f;
	public const float DeleteriousMutationRate = 0f;
	#endregion
	
	// Instance vars
	public FindMateDelegate FindMate = MateSelectionStrategies.FindFirstAvailableMate;
	public ReproduceDelegate Reproduce = ReproductionStrategies.SexualReproduce;
	public InitialPopulationGeneratorDelegate InitializePopulation =
		InitialPopulationGeneration.WorkingInitialPopulationThatChangesALot;
}